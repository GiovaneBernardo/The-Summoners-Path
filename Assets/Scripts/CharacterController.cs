
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
    public float gravity = -9.81f;
    public float totalGravity = 0.0f;
    public float sensitivity = 0.3f;
    public float speed = 8.0f;
    public float jumpHeight = 40.0f;
    public float maxSlopeAngle = 45;
    public float moveSpeed = 18.0f;
    public float groundDrag = 5.0f;
    public float jumpForce = 1300.0f;
    public float airMultiplier = 1.3f;
    public Vector3 bookIdlePosition = new Vector3(0.3f, -0.15f, 0.2f);
    public Vector3 bookIdleScale = new Vector3(6.67f, 5.0f, 0.1f);
    public Vector3 bookReadPosition = new Vector3(0.170, 0.0f, 0.0f);
    public Vector3 bookReadScale = new Vector3(8.0f, 6.0f, 0.2f);
    public Vector3 startPosition = new Vector3(0.0f, 9.792f, 475.89f);
    public UInt64 terrainUuid = 0;
    public UInt64 previewUuid = 0;
    public float heightSpawn = 50.0f;
    public DateTime lastJumpTime;
    public DateTime lastSummonTime;
    public DateTime startTime;
    public int maxSeconds = 60;

    public UInt64 errorMaterialUuid = 1654307431041515234;
    public UInt64 previewMaterialUuid = 2522693449534569271;


    public Vector3 LerpVector(Vector3 start, Vector3 end, float time)
    {
        return start + (end - start) * time;
    }

    public float lerpSpeed = 0.01f;
    public float lerpTime = 1.0f;
    bool lastFrameRightButtonPressed = false;
    public void OnStart()
    {
        gravity = -9.81f;
        totalGravity = 0.0f;
        sensitivity = 0.3f;
        speed = 24000.0f;
        jumpHeight = 40.0f;
        maxSlopeAngle = 45;
        moveSpeed = 18.0f;
        groundDrag = 5.0f;
        jumpForce = 1300.0f;
        airMultiplier = 1.3f;
        heightSpawn = 60.0f;
        maxSeconds = 60;


        Cursor.Hide();
        terrainUuid = FindEntityByName("Terrain").Uuid;
        previewUuid = FindEntityByName("FlyingObjectPreviewHolder").Uuid;

        this.AddComponent<TextRenderer>();
        startTime = DateTime.Now;
    }
    public bool canRespawn = false;
    public void OnUpdate()
    {

        this.GetComponent<TextRenderer>().SetText((maxSeconds - (DateTime.Now - startTime).TotalSeconds).ToString(), 0, 0, 3);
        if ((startTime - DateTime.Now).TotalSeconds < -maxSeconds)
            canRespawn = true;

        if (canRespawn)
        {
            this.GetComponent<RigidBody>().ApplyForce(new Vector3(0.0f));
            this.GetComponent<Transform>().Translation = startPosition;

            startTime = DateTime.Now;
            canRespawn = false;
        }
        else
        {
            MovePlayer();
            RotateCamera();
            SpeedControl();

            HandleBook();
        }
    }

    public void HandleBook()
    {
        bool isSummonCooldownOver = (lastSummonTime - DateTime.Now).TotalSeconds < -1.0f;

        RaycastHit hit;
        Physics_Raycast(this.GetComponent<Transform>().Translation + FindEntityByName("CameraEntity").GetComponent<Transform>().Translation, FindEntityByName("CameraEntity").GetComponent<Transform>().LeftVector * new Vector3(-1.0f), 10000.0f, out hit, previewUuid);
        /* Spawn swords */
        if (isSummonCooldownOver && Input.IsMouseDown(0))
        {
            Console.WriteLine(hit.hitUuid);
            if (hit.hitUuid == terrainUuid)
            {
                Entity newTruck = Instantiate(FindEntityByName("SwordSample"));
                newTruck.GetComponent<Transform>().Translation = new Entity(hit.hitUuid).GetComponent<Transform>().Translation + hit.point + new Vector3(0.0f, heightSpawn, 0.0f);

                InternalCalls.GetWorldRotationQuaternionCall(FindEntityByName("FlyingObjectPreviewSword").Uuid, out Vector4 rotation);
                Quaternion rotationQuaternion = new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);
                rotationQuaternion.Normalize();
                newTruck.GetComponent<Transform>().Rotation = rotationQuaternion;

                TravellerObjectsManager.flyingObjects.Add(newTruck.Uuid);
                lastSummonTime = DateTime.Now;
            }
        }
        else if (hit.hitUuid == terrainUuid)
        {
            FindEntityByName("FlyingObjectPreviewHolder").GetComponent<Transform>().Translation = new Entity(hit.hitUuid).GetComponent<Transform>().Translation + hit.point + new Vector3(0.0f, heightSpawn, 0.0f);
            FindEntityByName("FlyingObjectPreviewHolder").GetComponent<Transform>().Rotation = this.GetComponent<Transform>().Rotation;
        }

        FindEntityByName("FlyingObjectPreviewSword").GetComponent<MeshRenderer>().SetMaterial(isSummonCooldownOver ? previewMaterialUuid : errorMaterialUuid);

        ReadBook();

        ThrowPower();
    }

    public void ThrowPower()
    {

    }

    public void ReadBook()
    {
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
        if (collidedUuid == terrainUuid)
            canRespawn = true;
        //this.GetComponent<Transform>().Translation = startPosition;
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
        MoveWithSword(hit);

        if (hit.hitUuid == terrainUuid)
            canRespawn = true;

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
        force += new Vector3(0.0f, totalGravity, 0.0f);

        if (OnSlope(hit))
        {
            this.GetComponent<RigidBody>().AddForce(GetSlopeMoveDirection(hit, force), ForceMode.FORCE);
        }
        else
        {
            if (hittingGround)
                this.GetComponent<RigidBody>().AddForce(force, ForceMode.FORCE);
            else
                this.GetComponent<RigidBody>().AddForce(force * airMultiplier, ForceMode.FORCE);
        }

        bool isCoolDownOver = (lastJumpTime - DateTime.Now).TotalSeconds < -1.0f;
        if (hittingGround)
        {
            totalGravity = gravity;
            this.GetComponent<RigidBody>().drag = groundDrag;
            if (Input.IsKeyDown(KeyCode.Space) && isCoolDownOver)
            {
                //this.GetComponent<RigidBody>().ApplyForce(new Vector3(0.0f, jumpHeight, 0.0f));
                Jump();
                lastJumpTime = DateTime.Now;
            }
        }
        else
        {
            this.GetComponent<RigidBody>().drag = 0.0f;
            totalGravity += gravity;
        }

    }

    public void Jump()
    {
        RigidBody rigidBody = this.GetComponent<RigidBody>();
        rigidBody.velocity = new Vector3(rigidBody.velocity.X, 0.0f, rigidBody.velocity.Z);

        rigidBody.AddForce(new Vector3(0.0f, 1.0f, 0.0f) * jumpForce, ForceMode.IMPULSE);

    }

    public void SpeedControl()
    {
        Vector3 rigidBodyVelocity = this.GetComponent<RigidBody>().velocity;
        Vector3 flatVelocity = new Vector3(rigidBodyVelocity.X, 0.0f, rigidBodyVelocity.Z);
        if (Vector3.Magnitude(flatVelocity) > moveSpeed)
        {
            Vector3 limitedVelocity = Vector3.Normalize(flatVelocity) * moveSpeed;
            this.GetComponent<RigidBody>().velocity = new Vector3(limitedVelocity.X, rigidBodyVelocity.Y, limitedVelocity.Z);
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

    public void MoveWithSword(RaycastHit hit)
    {

        if (hit.hitUuid == 0 || InternalCalls.EntityGetName(hit.hitUuid) != "SwordSample")
            return;
        this.GetComponent<Transform>().Translation += new Entity(hit.hitUuid).GetComponent<Transform>().MoveTowardsReturn(new Vector3(0.0f, TravellerObjectsManager.flyingObjectsSpeed * Time.deltaTime, 0.0f));//new Entity(hit.hitUuid).GetComponent<Transform>().LeftVector * -1.0f * new Vector3(0.0f, 0.0f, TravellerObjectsManager.flyingObjectsSpeed * Time.deltaTime);
    }

    public void RotateCamera()
    {
        Quaternion quat = new Quaternion(new Vector3(0.0f, -Input.MouseDeltaX() * sensitivity * Time.deltaTime, 0.0f));
        this.GetComponent<Transform>().Rotation *= quat;

        FindEntityByName("CameraEntity").GetComponent<Transform>().Rotation *= new Quaternion(new Vector3(0.0f, 0.0f, -Input.MouseDeltaY() * sensitivity * Time.deltaTime));
    }
}
