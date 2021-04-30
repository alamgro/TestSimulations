using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/*
 * BUG LIST :c
 * -[Comportamiento]: Hay un bug que no he replicado al 100%, sucede cuando el minión ya rotó cuando llega a un borde, sin embargo,
 * no logró girar lo suficiente como para poder caminar hacia el lado contrario del borde. [Conclusión]: El minion se deja de
 * mover eternamente pero siempre está detectando que hay borde, así que no creo que ese sea el problema. Debe ser la lógica en las
 * condiciones de movimiento.
 * 
 * -No es bug, pero estaría bien que no avance a la vez que rota cuando hay una caída en frente. (Que únicamente rote, y una vez acabado, siga avanzando)
 */


public class Minion : MonoBehaviour
{
    #region PUBLIC VARIABLES
    [Header("Player config")]
    public Transform fallChecker; //The Transform in front of the minion, this is the origin of the raycast to check if there is a fall in front.
    public LayerMask layerIgnoreSelf; //Layer para checar si el minion está en el piso, detecta todas las layers menos la del mismo minion
    [Header("Rotation parameters")]
    public float rotationSmooth; //Determina qué tan suave rota (entre más grande el número, más rápido rotará).
    public float minTimeToRotate, maxTimeToRotate; //Rango de tiempo en el que el objeto podría volver a cambiar de rotación.
    [Header("Movement parameters")]
    public float moveSpeed; //Velocidad a la que se mueve.
    public float jumpForce; //Fuerza de salto.
    public float minTimeToMove, maxTimeToMove; //Rango de tiempo en el que el objeto podría caminar, o decidir quedarse parado.
    #endregion

    #region PRIVATE VARIABLES
    private Rigidbody rb;
    private Quaternion targetRotation; //Guarda la rotación a la que el minion debe girar (si decide hacerlo)
    private float timerRotation; //Timer para la cuenta regresiva de la rotación, cuando llega a 0 genera otra rotación para el minion.
    private float timerMovement; //Timer para la cuenta regresiva del movimiento, cuando llega a 0 el minion decide si moverse o no.
    private float currentSpeed; //Velocidad actual del minion, esta se le asigna al rb.velocity para controlar la velocidad cada frame.
    private Vector3 tempVelocity; //Guarda temporalmente el velocity del rigidbody para modificarlo sobre la marcha
    private bool needNewRotation = true; //Hace que solo se genere un target, esto hasta que el minion haya terminado de rotar hasta el target.
    private bool stayInPlace; //Cuando su valor es true indica que el objeto debe de permanecer sin moverse.
    private float distToGround; //Distancia que hay hacia el piso desde el centro del objeto.
    #endregion

    void Start()
    {
        #region GET COMPONENTS
        rb = GetComponent<Rigidbody>(); //Obtener Rigidbody
        gameObject.GetComponent<MeshRenderer>().material.color = Random.ColorHSV(); //Cambiar el color
        distToGround = GetComponent<Collider>().bounds.extents.y;
        #endregion

        currentSpeed = moveSpeed; //Inicializar la velocidad actual, con el valor default
        timerRotation = timerMovement = 0f;
        transform.rotation = GenerateRotation(); //Empieza con un rotación aleatoria
    }

    void Update()
    {
        #region ***DEBUG***
        //Hacer saltar el minion
        if(Input.GetKeyDown(KeyCode.Space))
            Jump();
        //Raycast de IsGrounded();
        Debug.DrawRay(transform.position, Vector3.down * (distToGround + 0.05f), Color.blue);
        #endregion

        //Mientras no deba rotar, entonces puede seguir restando el tiempo para que no se generen más rotaciones antes de que acabe la que tiene pendiente
        if (!ShouldRotate())
        {
            needNewRotation = true; //Quiere decir que podría necesitar rotar en caso de encontrarse con un borde debajo
            timerRotation -= Time.deltaTime; //Restar a la cuenta regresiva, llegado a 0 genera otra rotación
        }
        currentSpeed = 0f; //Por defecto la velocidad es 0 hasta que las condiciones lo cambien
        if (CheckFrontGround()) //
        {
            Debug.DrawRay(fallChecker.position, Vector3.down * 2.51f, Color.green);
            timerMovement -= Time.deltaTime; //Restar a la cuenta regresiva, llegado a 0 decide si moverse o no

            //Checar timers para rotar y/o moverse
            CheckMoveTimer();
            CheckRotationTimer();

            if (stayInPlace)
            {
                CheckFrontObstacle(); //Revisar si hay algo que puede brincar
                currentSpeed = moveSpeed;
            }
            //currentSpeed = stayInPlace ? 0 : moveSpeed; //Si decide no meverse, entonces la velocidad es 0 ----
        }
        else 
        {
            Debug.DrawRay(fallChecker.position, Vector3.down * 2.51f, Color.red);
            //currentSpeed = 0f; ----
            if (needNewRotation)
            {
                needNewRotation = false;
                targetRotation = transform.rotation * GenerateRotation(180f);
            }
            
            //targetRotation.eulerAngles = transform.rotation.eulerAngles + GenerateRotation(180f).eulerAngles;
            //print("Inversa del angulo -> " + Quaternion.Inverse(GenerateRotation(75f)).eulerAngles);
        }

        Rotate();
    }

    private void FixedUpdate()
    {
        Move(currentSpeed); //Mover el player a la velocidad indicada
    }

    private void Move(float _moveSpeed)
    {
        tempVelocity = transform.worldToLocalMatrix.inverse * Vector3.forward * _moveSpeed;
        tempVelocity.y = rb.velocity.y;

        rb.velocity = tempVelocity;
    }

    private void Rotate()
    {
        //Verifica si se debe rotar, para que no se esté haciendo todo el tiempo
        if (ShouldRotate())
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSmooth * Time.deltaTime);
        }
    }

    private void Jump()
    {
        //rb.AddForce(Vector2.up * jumpForce, ForceMode.Impulse);
        rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
    }

    private bool ShouldRotate()
    {
        return transform.rotation != targetRotation;
    }

    private bool CheckFrontGround()
    {
        return Physics.Raycast(fallChecker.position, Vector3.down, 2.51f);
    }

    /// <summary>
    /// Verifica si el Minion está en el suelo (grounded).
    /// </summary>
    /// <returns>
    /// Devuelve true cuando está tocando el suelo.
    /// </returns>
    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, distToGround + 0.05f, layerIgnoreSelf);
    }

    private void CheckFrontObstacle()
    {
        if (Physics.Raycast(fallChecker.position, Vector3.down, out RaycastHit hit, 1.5f))
        {
            Debug.Log("Something is in front of me...");
            //Aquí verifica que choque con algo que puede brincar
            if (IsGrounded() && Vector3.Distance(fallChecker.position, hit.point) > 0.5f)
            {
                //Debug.DrawRay(fallChecker.position, Vector3.down * 1.5f, Color.yellow);
                Debug.Log("Jumping now!");
                Jump();
            }
            else
            {
                print("I'm flying!");
                rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            }
        }
    }

    private void CheckRotationTimer()
    {
        if (timerRotation <= 0f)
        {
            timerRotation = Random.Range(minTimeToRotate, maxTimeToRotate);
            targetRotation = GenerateRotation();
        }
    }

    private void CheckMoveTimer()
    {
        if (timerMovement <= 0f)
        {
            timerMovement = Random.Range(minTimeToMove, maxTimeToMove);
            stayInPlace = (1 == Random.Range(0, 2)); //Genera verdadero o falso al azar
        }
    }

    /// <summary>
    /// Genera una rotación aleatoria de tipo Quaternion.
    /// </summary>
    /// <returns>
    /// Un valor flotante que va desde 0 a 180 grados.
    /// </returns>
    private Quaternion GenerateRotation()
    {
        return Quaternion.Euler(0f, Random.Range(0f, 180f), 0f);
    }

    /// <summary>
    /// Genera una rotación de tipo Quaternion.
    /// </summary>
    /// <param name="_rotationAngle">Cantidad de grados euler que va a girar.</param>
    private Quaternion GenerateRotation(float _rotationAngle)
    {
        return Quaternion.Euler(0f, _rotationAngle, 0f);
    }


}
