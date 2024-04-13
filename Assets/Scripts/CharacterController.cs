
using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plaza;
using static Plaza.InternalCalls;
using static Plaza.Input;
using static Plaza.Physics;

public class CharacterController : Entity
{
    public float gravity = -79.81f;
    public float totalGravity = 0.0f;
    public float sensitivity = 0.3f;
    public float speed = 3000000.0f;
    public float maxSlopeAngle = 45;
    public Vector3 bookIdlePosition = new Vector3(0.3f, -0.15f, 0.2f);
    public Vector3 bookIdleScale = new Vector3(6.67f, 5.0f, 0.1f);
    public Vector3 bookReadPosition = new Vector3(0.170, 0.0f, 0.0f);
    public Vector3 bookReadScale = new Vector3(8.0f, 6.0f, 0.2f);
    public Vector3 startPosition = new Vector3(0.0f, 9.792f, 475.89f);
    public UInt64 terrainUuid = 0;
    public float truckHeight = 5.0f;
    public DateTime lastJumpTime;

    public Vector3 LerpVector(Vector3 start, Vector3 end, float time)
    {
        return start + (end - start) * time;
    }

    public float lerpSpeed = 0.01f;
    public float lerpTime = 1.0f;
    bool lastFrameRightButtonPressed = false;
    public void OnStart()
    {
        Cursor.Hide();
        terrainUuid = FindEntityByName("Terrain").Uuid;
    }

    public void OnUpdate()
    {
        speed = 3000000.0f;
        gravity = -79.81f;


        MovePlayer();
        RotateCamera();

        HandleBook();
    }

    public void HandleBook()
    {
        RaycastHit hit;
        Physics_Raycast(this.GetComponent<Transform>().Translation + FindEntityByName("CameraEntity").GetComponent<Transform>().Translation, FindEntityByName("CameraEntity").GetComponent<Transform>().LeftVector * new Vector3(-1.0f), 10000.0f, out hit, this.Uuid);
        /* Spawn trucks */
        if (Input.IsMouseDown(0))
        {
            //FindEntityByName("Sphere").GetComponent<Transform>().Translation = this.GetComponent<Transform>().Translation + (new Vector3(5.0f, 0.0f, 0.0f) * FindEntityByName("CameraEntity").GetComponent<Transform>().Rotation);
            Console.WriteLine(hit.hitUuid);
            if (hit.hitUuid == terrainUuid)
                Instantiate(FindEntityByName("Truck")).GetComponent<Transform>().Translation = new Entity(hit.hitUuid).GetComponent<Transform>().Translation + hit.point + new Vector3(0.0f, truckHeight, 0.0f);
        }
        else if (hit.hitUuid == terrainUuid)
            FindEntityByName("TruckPreview").GetComponent<Transform>().Translation = new Entity(hit.hitUuid).GetComponent<Transform>().Translation + hit.point + new Vector3(0.0f, truckHeight, 0.0f);

        /* Lerp Positions */
        if (lastFrameRightButtonPressed)
            lerpTime += lerpSpeed * Time.deltaTime;
        else
            lerpTime -= lerpSpeed * Time.deltaTime;
        lerpTime = Math.Min(lerpTime, 1.0f);
        lerpTime = Math.Max(lerpTime, 0.0f);
        Transform pageTransform = FindEntityByName("page").GetComponent<Transform>();
        if (Input.IsMouseDown(1))
        {
            pageTransform.Translation = LerpVector(bookReadPosition, pageTransform.Translation, lerpTime);
            pageTransform.Scale = LerpVector(bookReadScale, pageTransform.Scale, lerpTime);
            lastFrameRightButtonPressed = true;
        }
        else
        {
            pageTransform.Translation = LerpVector(bookIdlePosition, pageTransform.Translation, lerpTime);
            pageTransform.Scale = LerpVector(bookIdleScale, pageTransform.Scale, lerpTime);
            lastFrameRightButtonPressed = false;
        }
    }

    public void OnCollide(UInt64 collidedUuid, Vector3 hitPosition)
    {
        //if (collidedUuid == terrainUuid)
        //    this.GetComponent<Transform>().Translation = startPosition;
    }

    public void OnRestart()
    {

    }


    public void MovePlayer()
    {
        Vector3 force = new Vector3(0.0f);
        float vertical = 0.0f;
        float horizontal = 0.0f;
        Vector3 movement = new Vector3(0.0f, totalGravity, 0.0f);

        Transform transform = this.GetComponent<Transform>();

        RaycastHit hit;
        Physics_Raycast(this.GetComponent<Transform>().Translation, new Vector3(0.0f, -1.0f, 0.0f), 0.7f, out hit, this.Uuid);

        bool hittingGround = hit.hitUuid != 0;

        if (Input.IsKeyDown(KeyCode.W))
        {
            vertical += speed;
        }
        if (Input.IsKeyDown(KeyCode.S))
        {
            vertical += -speed;
        }
        if (Input.IsKeyDown(KeyCode.A))
        {
            horizontal += -speed;
        }
        if (Input.IsKeyDown(KeyCode.D))
        {
            horizontal += speed;
        }

        force = this.GetComponent<Transform>().LeftVector * -vertical + this.GetComponent<Transform>().ForwardVector * horizontal;

        if (!hittingGround)
            force *= 1.0f / 10.0f;

        if (OnSlope(hit))
        {
            this.GetComponent<RigidBody>().AddForce(GetSlopeMoveDirection(hit, force) * (Time.deltaTime), ForceMode.FORCE);
        }
        else
            this.GetComponent<RigidBody>().AddForce(force * Time.deltaTime, ForceMode.FORCE);

        bool isCoolDownOver = (lastJumpTime - DateTime.Now).TotalSeconds < -1.0f;
        if (hittingGround)
        {
            totalGravity = gravity;
            this.GetComponent<RigidBody>().drag = 7.0f;
            if (Input.IsKeyDown(KeyCode.Space) && isCoolDownOver)
            {
                this.GetComponent<RigidBody>().ApplyForce(new Vector3(0.0f, 40.0f, 0.0f));
                lastJumpTime = DateTime.Now;
            }
        }
        else
        {
            this.GetComponent<RigidBody>().drag = 0.0f;
            totalGravity += gravity * Time.deltaTime;
        }
    }

    private bool OnSlope(RaycastHit hit)
    {
        if (hit.hitUuid == 0)
            return false;
        float angle = Vector3.Angle(new Vector3(0.0f, 1.0f, 0.0f), hit.normal);
        return angle < maxSlopeAngle && angle != 0;
    }

    private Vector3 GetSlopeMoveDirection(RaycastHit hit, Vector3 moveDirection)
    {
        return Vector3.ProjectOnPlane(moveDirection, hit.normal);
    }

    public void RotateCamera()
    {
        Quaternion quat = new Quaternion(new Vector3(0.0f, -Input.MouseDeltaX() * sensitivity * Time.deltaTime, 0.0f));
        this.GetComponent<Transform>().Rotation *= quat;

        FindEntityByName("CameraEntity").GetComponent<Transform>().Rotation *= new Quaternion(new Vector3(0.0f, 0.0f, -Input.MouseDeltaY() * sensitivity * Time.deltaTime));
    }
}
