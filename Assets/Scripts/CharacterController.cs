
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
using System.Runtime.Remoting.Services;

public class CharacterController : Entity
{
    public static int swordsCount = 8;
    public float gravity = -9.81f;
    public float totalGravity = 0.0f;
    public float sensitivity = 0.1f;
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
    public Vector3 bookReadScale = new Vector3(4.0f, 3.0f, 0.2f);
    public Vector3 startPosition = new Vector3(0.0f, 9.792f, 475.89f);
    public UInt64 terrainUuid = 0;
    public UInt64 previewUuid = 0;
    public UInt64 towerUuid = 0;
    public float heightSpawn = 20.0f;
    public DateTime lastJumpTime;
    public DateTime lastSummonTime;
    public DateTime lastBallSummonTime;
    public DateTime startTime;
    public TimeSpan bestTime = new TimeSpan(0);
    public int winCount = 0;
    public int maxSeconds = 180;

    public UInt64 errorMaterialUuid = 1654307431041515234;
    public UInt64 previewMaterialUuid = 2522693449534569271;
    public UInt64 entityToInstantiateUuid = 0;
    public UInt64 ballToInstantiateUuid = 0;
    public UInt64 redBallPreviewUuid = 0;

    public Vector3 platformPosition = new Vector3(0.0f, 5.829f, 476.451f);
    public Vector3 endPosition = new Vector3(-120, -51, -665);

    public Vector2 swordsCountTextPos = new Vector2(0.0f, 0.0f);
    public Vector2 bestTimeTextPos = new Vector2(0.0f, 50.0f);
    public Vector2 sensitivityTextPos = new Vector2(0.0f, 500.0f);

    public Vector3 LerpVector(Vector3 start, Vector3 end, float time)
    {
        return start + (end - start) * time;
    }

    public float lerpSpeed = 0.07f;
    public float lerpTime = 1.0f;
    bool lastFrameRightButtonPressed = false;

    public bool canRespawn = false;
    public bool platformIsOnStart = false;
    public bool showMenu = true;

    public bool airJumpAvailable = true;
    public int jumpCount = 2;
    public DateTime lastMuteTime;
    public void OnStart()
    {
        gravity = -9.81f;
        totalGravity = 0.0f;
        sensitivity = 0.1f;
        speed = 24000.0f;
        jumpHeight = 40.0f;
        maxSlopeAngle = 45;
        moveSpeed = 18.0f;
        groundDrag = 5.0f;
        jumpForce = 1300.0f;
        airMultiplier = 1.3f;
        heightSpawn = 20.0f;
        maxSeconds = 180;

        lerpSpeed = 0.07f;

        //bookIdlePosition = new Vector3(0.3f, -0.15f, 0.2f);
        //bookIdleScale = new Vector3(6.0f, 3.0f, 0.1f);
        //bookReadPosition = new Vector3(0.170, 0.0f, 0.0f);
        //bookReadScale = new Vector3(6.0f, 3.0f, 0.2f);
        //startPosition = new Vector3(0.0f, 9.792f, 475.89f);

        Cursor.Hide();
        terrainUuid = FindEntityByName("Terrain").Uuid;
        previewUuid = FindEntityByName("FlyingObjectPreviewHolder").Uuid;
        towerUuid = FindEntityByName("tower").Uuid;
        entityToInstantiateUuid = FindEntityByName("SwordSample").Uuid;
        ballToInstantiateUuid = FindEntityByName("RedBall").Uuid;
        redBallPreviewUuid = FindEntityByName("RedBallPreview").Uuid;

        this.AddComponent<TextRenderer>();
        FindEntityByName("SwordsCountText").AddComponent<TextRenderer>();
        FindEntityByName("SensitivityText").AddComponent<TextRenderer>();
        startTime = DateTime.Now;

        canRespawn = true;

        FindEntityByName("MusicEntity").GetComponent<AudioSource>().Play();
    }

    public bool muteMusic = false;
    public void OnUpdate()
    {
         if (showMenu)
         {
             if (Input.IsAnyKeyPressed() || Input.IsMouseDown(0))
             {
                 ReadBook();
                 showMenu = false;
             }
            return;
         }
        //this.GetComponent<TextRenderer>().SetText((maxSeconds - (DateTime.Now - startTime).TotalSeconds).ToString(), 0, 0, 3);
        FindEntityByName("SwordsCountText").GetComponent<TextRenderer>().SetText("Swords: " + swordsCount.ToString(), swordsCountTextPos.X, swordsCountTextPos.Y, 3);
        if (winCount > 0)
            FindEntityByName("BestTimeText").GetComponent<TextRenderer>().SetText("Best Time: " + ((decimal)bestTime.TotalSeconds).ToString("F2") + "s", bestTimeTextPos.X, bestTimeTextPos.Y, 3);

        if (CharacterController.swordsCount <= -1 || this.GetComponent<Transform>().Translation.Y < -100.0f) //(startTime - DateTime.Now).TotalSeconds < -maxSeconds || 
            canRespawn = true;

        if (canRespawn)
        {
            PlayDeathSound();

            FindEntityByName("Platform").GetComponent<Transform>().Translation = platformPosition;
            this.GetComponent<RigidBody>().ApplyForce(new Vector3(0.0f));
            this.GetComponent<RigidBody>().velocity = new Vector3(0.0f);
            this.GetComponent<Transform>().Translation = startPosition;
            TravellerObjectsManager.Reset(startPosition);

            startTime = DateTime.Now;
            canRespawn = false;
            CharacterController.swordsCount = 8;
            platformIsOnStart = true;
        }
        else
        {
            MovePlayer();
            RotateCamera();
            SpeedControl();

            HandleBook();
        }
        // Check if player reached the end
        //if (Vector3.Distance(this.GetComponent<Transform>().Translation, endPosition) < 250.0f)
        //{
        //    Win();
        //}

        // Move the platform away if player moved
        if (platformIsOnStart && Vector3.Distance(this.GetComponent<Transform>().Translation, platformPosition) > 25.0f)
        {
            FindEntityByName("Platform").GetComponent<Transform>().Translation = new Vector3(0.0f, -1000.0f, 0.0f);
            platformIsOnStart = false;
        }

        bool isMuteCoolDownOver = (lastMuteTime - DateTime.Now).TotalSeconds < -0.3f;
        if (isMuteCoolDownOver && muteMusic && Input.IsKeyDown(KeyCode.M))
        {
            FindEntityByName("MusicEntity").GetComponent<AudioSource>().Play();
            muteMusic = !muteMusic;
            lastMuteTime = DateTime.Now;
        }
        else if (isMuteCoolDownOver && !muteMusic && Input.IsKeyDown(KeyCode.M))
        {
            FindEntityByName("MusicEntity").GetComponent<AudioSource>().Stop();
            muteMusic = !muteMusic;
            lastMuteTime = DateTime.Now;
        }
    }

    public static void PlayDeathSound()
    {
        InternalCalls.AudioSource_Play(FindEntityByNameCall("DeathSoundEntity"));
    }

    public static void PlaySwordSummonSound()
    {
        InternalCalls.AudioSource_Play(FindEntityByNameCall("SwordSummonSoundEntity"));
    }

    public static void PlaySwordPointSound()
    {
        InternalCalls.AudioSource_Play(FindEntityByNameCall("SwordHitSoundEntity"));
    }

    public static void PlayFallSound()
    {
        InternalCalls.AudioSource_Play(FindEntityByNameCall("FallSoundEntity"));
    }

    public static void PlayWinSound()
    {
        InternalCalls.AudioSource_Play(FindEntityByNameCall("WinSoundEntity"));
    }

    public static void PlayPowerSound()
    {
        InternalCalls.AudioSource_Play(FindEntityByNameCall("UsePowerSoundEntity"));
    }

    public static void PlayJumpSound()
    {
        InternalCalls.AudioSource_Play(FindEntityByNameCall("JumpSoundEntity"));
    }

    public static void PlayFailSound()
    {
        InternalCalls.AudioSource_Play(FindEntityByNameCall("FailSoundEntity"));
    }

    public void Win()
    {
        PlayWinSound();
        bestTime = (DateTime.Now - startTime).TotalSeconds < bestTime.TotalSeconds || bestTime == new TimeSpan(0)  ? (DateTime.Now - startTime) : bestTime;
        winCount++;

        if (winCount == 1)
            FindEntityByName("BestTimeText").AddComponent<TextRenderer>();
        canRespawn = true;
    }

    public void HandleBook()
    {
        bool isSummonCooldownOver = (lastSummonTime - DateTime.Now).TotalSeconds < -1.0f;

        RaycastHit hit;
        Physics_RaycastSpecific(this.GetComponent<Transform>().Translation + FindEntityByName("CameraEntity").GetComponent<Transform>().Translation, FindEntityByName("CameraEntity").GetComponent<Transform>().LeftVector * new Vector3(-1.0f), 10000.0f, out hit, terrainUuid);
        /* Spawn swords */
        if (isSummonCooldownOver && Input.IsMouseDown(0))
        {
            if (hit.hitUuid == terrainUuid && CharacterController.swordsCount > 0)
            {
                PlaySwordSummonSound();

                Entity newTruck = Instantiate(new Entity(entityToInstantiateUuid));
                newTruck.GetComponent<Transform>().Translation = FindEntityByName("FlyingObjectPreviewHolder").GetComponent<Transform>().Translation;//new Entity(hit.hitUuid).GetComponent<Transform>().Translation + hit.point + new Vector3(0.0f, heightSpawn, 0.0f);

                InternalCalls.GetWorldRotationQuaternionCall(FindEntityByName("FlyingObjectPreviewSword").Uuid, out Vector4 rotation);
                Quaternion rotationQuaternion = new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);
                rotationQuaternion.Normalize();
                newTruck.GetComponent<Transform>().Rotation = rotationQuaternion;

                TravellerObjectsManager.flyingObjects.Add(newTruck.Uuid);
                CharacterController.swordsCount--;
                lastSummonTime = DateTime.Now;
            }
        }
        else if (hit.hitUuid == terrainUuid)
        {
            if (CharacterController.swordsCount <= 0 && Input.IsMouseDown(0))
                PlayFailSound();
            FindEntityByName("FlyingObjectPreviewHolder").GetComponent<Transform>().Translation = hit.point + new Vector3(0.0f, heightSpawn, 0.0f);

            /* Calculate yaw */
            float yaw = QuaternionToEulerAngles(this.GetComponent<Transform>().Rotation).Y * Mathf.Rad2Deg;
            if (yaw > 0.0f && yaw < 180.0f) // Only rotate if yaw is not heading to the end
            {
                yaw = yaw * Mathf.Deg2Rad;
                yaw = Clamp(yaw, 0.0f, 180.0f);
                FindEntityByName("FlyingObjectPreviewHolder").GetComponent<Transform>().Rotation = Vector3ToQuaternion(new Vector3(0.0f, yaw, 0.0f));//this.GetComponent<Transform>().Rotation;
            }

        }

        FindEntityByName("FlyingObjectPreviewSword").GetComponent<MeshRenderer>().SetMaterial(isSummonCooldownOver && CharacterController.swordsCount > 0 ? previewMaterialUuid : errorMaterialUuid);

        ReadBook();

        ThrowPower();

        HandleSensitivityControl();
    }

    public void ThrowPower()
    {
        bool isBallSummonCooldownOver = (lastBallSummonTime - DateTime.Now).TotalSeconds < -0.7f;

        if (isBallSummonCooldownOver && Input.IsMouseDown(1))
        {
            if (platformIsOnStart)
            {
                PlayFailSound();
                return;
            }

            PlayPowerSound();
            Entity newBall = Instantiate(new Entity(ballToInstantiateUuid));
            newBall.GetComponent<Transform>().Translation = FindEntityByName("CameraEntity").GetComponent<Transform>().Translation + FindEntityByName("Player").GetComponent<Transform>().Translation;//new Entity(hit.hitUuid).GetComponent<Transform>().Translation + hit.point + new Vector3(0.0f, heightSpawn, 0.0f);

            InternalCalls.GetWorldRotationQuaternionCall(redBallPreviewUuid, out Vector4 rotation);
            Quaternion rotationQuaternion = new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);
            rotationQuaternion.Normalize();
            newBall.GetComponent<Transform>().Rotation = rotationQuaternion;
            TravellerObjectsManager.flyingBalls.Add(newBall.Uuid);
            lastBallSummonTime = DateTime.Now;
        }
        else
        {
            Entity redBallHolder = FindEntityByName("RedBallHolder");
            redBallHolder.GetComponent<Transform>().Translation = FindEntityByName("CameraEntity").GetComponent<Transform>().Translation + FindEntityByName("Player").GetComponent<Transform>().Translation;//new Entity(hit.hitUuid).GetComponent<Transform>().Translation + hit.point + new Vector3(0.0f, heightSpawn, 0.0f);
            redBallHolder.GetComponent<Transform>().Rotation = this.GetComponent<Transform>().Rotation;

            Entity redBallPreview = new Entity(redBallPreviewUuid);
            redBallPreview.GetComponent<Transform>().Rotation = FindEntityByName("CameraEntity").GetComponent<Transform>().Rotation;
        }
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
        //if (showMenu)
        //{
        //    pageTransform.Translation = LerpVector(bookReadPosition, pageTransform.Translation, lerpTime);
        //    pageTransform.Scale = LerpVector(bookReadScale, pageTransform.Scale, lerpTime);
        //    lastFrameRightButtonPressed = true;
        //}
        //else
        //{
        pageTransform.Translation = LerpVector(bookIdlePosition, pageTransform.Translation, lerpTime);
        pageTransform.Scale = LerpVector(bookIdleScale, pageTransform.Scale, lerpTime);
        lastFrameRightButtonPressed = false;
        //}
    }

    public void OnCollide(UInt64 collidedUuid, Vector3 hitPosition)
    {
        if (collidedUuid == terrainUuid)
            canRespawn = true;
        if (collidedUuid == towerUuid)
            Win();
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
        force *= Time.deltaTime * 100.0f;

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

        if (hittingGround)
        {
            totalGravity = gravity;
            this.GetComponent<RigidBody>().drag = groundDrag;

            airJumpAvailable = true;
        }
        else
        {
            this.GetComponent<RigidBody>().drag = 0.0f;
            totalGravity += gravity;
        }

        HandleJump(hittingGround);

    }

    public void HandleJump(bool hittingGround)
    {
        bool isJumpCoolDownOver = (lastJumpTime - DateTime.Now).TotalSeconds < -0.4f;
        if (isJumpCoolDownOver && hittingGround)
            jumpCount = 2;

        if (Input.IsKeyDown(KeyCode.Space) && isJumpCoolDownOver && jumpCount > 0)
        {
            //this.GetComponent<RigidBody>().ApplyForce(new Vector3(0.0f, jumpHeight, 0.0f));
            Jump();
            lastJumpTime = DateTime.Now;
            if (!hittingGround)
                airJumpAvailable = false;
            jumpCount--;
        }
    }

    public void Jump()
    {
        PlayJumpSound();
        totalGravity = 0.0f;
        RigidBody rigidBody = this.GetComponent<RigidBody>();
        rigidBody.velocity = new Vector3(rigidBody.velocity.X, 0.0f, rigidBody.velocity.Z);

        rigidBody.velocity = new Vector3(rigidBody.velocity.X, 0.0f, rigidBody.velocity.Z);
        rigidBody.ApplyForce(new Vector3(0.0f, 0.0f, 0.0f));
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

        Quaternion newRotation = FindEntityByName("CameraEntity").GetComponent<Transform>().Rotation * new Quaternion(new Vector3(0.0f, 0.0f, -Input.MouseDeltaY() * sensitivity * Time.deltaTime));
        newRotation.z = Clamp(newRotation.z, -45.0f * Mathf.Deg2Rad, 45.0f * Mathf.Deg2Rad);

        FindEntityByName("CameraEntity").GetComponent<Transform>().Rotation = newRotation;
    }

    public static float Clamp(float angle, float minAngle, float maxAngle)
    {
        return Math.Min(Math.Max(angle, minAngle), maxAngle);
    }

    public static Vector3 QuaternionToEulerAngles(Quaternion q)
    {
        // Extract sin/cos of yaw, pitch, roll angles
        float sinYaw = 2.0f * (q.w * q.y + q.x * q.z);
        float cosYaw = 1.0f - 2.0f * (q.y * q.y + q.z * q.z);
        float sinPitch = 2.0f * (q.w * q.x - q.y * q.z);
        float cosPitch = 1.0f - 2.0f * (q.x * q.x + q.z * q.z);

        // Calculate yaw (heading) angle
        float yaw = (float)Math.Atan2(sinYaw, cosYaw);

        // Calculate pitch angle
        float pitch;
        if (Math.Abs(sinPitch) >= 1)
            pitch = (float)Math.PI / 2 * Math.Sign(sinPitch); // Use +/-90 degrees if singularity
        else
            pitch = (float)Math.Asin(sinPitch);

        // No need to calculate roll for heading direction

        // Return Euler angles as Vector3
        return new Vector3(pitch, yaw, 0);
    }

    public static float CalculateYawFromQuaternion(Quaternion quaternion)
    {
        // Ensure the quaternion is normalized
        quaternion.Normalize();

        // Extract quaternion components
        float q_w = quaternion.w;
        float q_x = quaternion.x;
        float q_y = quaternion.y;
        float q_z = quaternion.z;

        // Calculate yaw angle
        float yaw = (float)Math.Atan2(2 * (q_w * q_z + q_x * q_y), 1 - 2 * (q_y * q_y + q_z * q_z));

        // Convert radians to degrees
        yaw = yaw * Mathf.Rad2Deg;

        // Ensure the yaw angle is within [0, 360) range
        if (yaw < 0)
        {
            yaw += 360;
        }

        return yaw;
    }

    public static Quaternion Vector3ToQuaternion(Vector3 axisRotations)
    {
        // Convert Euler angles to radians
        float yaw = axisRotations.Y * 0.5f;
        float pitch = axisRotations.X * 0.5f;
        float roll = axisRotations.Z * 0.5f;

        // Calculate sin/cos of half angles
        float sinYaw = (float)Math.Sin(yaw);
        float cosYaw = (float)Math.Cos(yaw);
        float sinPitch = (float)Math.Sin(pitch);
        float cosPitch = (float)Math.Cos(pitch);
        float sinRoll = (float)Math.Sin(roll);
        float cosRoll = (float)Math.Cos(roll);

        // Calculate quaternion components
        float x = cosYaw * sinPitch * cosRoll + sinYaw * cosPitch * sinRoll;
        float y = sinYaw * cosPitch * cosRoll - cosYaw * sinPitch * sinRoll;
        float z = cosYaw * cosPitch * sinRoll - sinYaw * sinPitch * cosRoll;
        float w = cosYaw * cosPitch * cosRoll + sinYaw * sinPitch * sinRoll;

        // Return the quaternion
        return new Quaternion(x, y, z, w);
    }

    public void HandleSensitivityControl()
    {
        if (Input.IsKeyDown(KeyCode.PageUp))
        {
            sensitivity = Clamp(sensitivity + (Math.Max(sensitivity * 0.10f, 0.03f) * Time.deltaTime), 0.01f, 2.0f);
            FindEntityByName("SensitivityText").GetComponent<TextRenderer>().SetText("Sensitivity: " + sensitivity.ToString("F3"), sensitivityTextPos.X, sensitivityTextPos.Y, 1.5f);;
        }
        else if (Input.IsKeyDown(KeyCode.PageDown))
        {
            sensitivity = Clamp(sensitivity - (Math.Max(sensitivity * 0.10f, 0.03f) * Time.deltaTime), 0.01f, 2.0f); ;
            FindEntityByName("SensitivityText").GetComponent<TextRenderer>().SetText("Sensitivity: " + sensitivity.ToString("F3"), sensitivityTextPos.X, sensitivityTextPos.Y, 1.5f); ;
        }
    }
}
