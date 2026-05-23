using UnityEngine;
using KBCore.Refs;
using Unity.Cinemachine;

[RequireComponent(typeof(Rigidbody), typeof(Animator))]
public class PlayerController : ValidatedMonoBehaviour
{
    [Header("References")]
    [SerializeField, Self] Rigidbody rb;
    [SerializeField, Self] GroundChecker groundChecker;
    [SerializeField, Self] Animator animator;
    [SerializeField, Self] PlatformCollisionHandler platformCollisionHandler;
    [SerializeField, Anywhere] CinemachineCamera freeLookVCam;
    [SerializeField, Anywhere] InputReader input;

    [Header("Movement Settings")]
    [SerializeField] float moveSpeed = 6f;
    [SerializeField] float rotationSpeed = 15f;
    [SerializeField] float smoothTime = 0.2f;

    [Header("Attack Settings")]
    [SerializeField] float attackCooldown = 0.5f;
    [SerializeField] float attackRange = 2f;
    [SerializeField] float attackDamage = 40f;

    [Header("Dash Settings")]
    [SerializeField, Min(1f)] float dashSpeedMultiplier = 2.5f;
    [SerializeField, Min(1f)] float sprintSpeedMultiplier = 1.5f;
    [SerializeField, Min(0.05f)] float dashDuration = 0.18f;
    [SerializeField, Min(0f)] float maxStamina = 4f;
    [SerializeField, Min(0f)] float staminaDrainPerSecond = 1f;
    [SerializeField, Min(0f)] float staminaRegenPerSecond = 1.5f;
    [SerializeField, Range(0f, 1f)] float minStaminaToDash = 0.2f;
    [SerializeField, Min(1)] int maxConsecutiveDashes = 2;

    [Header("Jump Settings")]
    [SerializeField, Min(0.05f)] float jumpDuration = 0.45f; 
    [SerializeField] float jumpCooldown = 0f;
    [SerializeField, Min(0.1f)] float jumpMaxHeight = 1.5f;
    [SerializeField, Range(1f, 5f)] float gravityMultiplier = 3f;
    [SerializeField, Range(1f, 4f)] float jumpCutGravityMultiplier = 2f;
    [SerializeField, Range(0f, 0.3f)] float jumpBufferTime = 0.12f;
    [SerializeField, Range(0f, 0.3f)] float coyoteTime = 0.1f;

    [Header("Animation Settings")]
    [SerializeField, Tooltip("Скорость по Y ниже этого значения включает состояние Fall.")]
    float fallVelocityThreshold = -0.05f;

    const float ZeroF = 0f;
    Transform mainCam;

    float currentSpeed;
    float velocity;
    float jumpVelocity;
    float jumpBufferCounter;
    float coyoteCounter;
    float jumpInitialVelocity;
    float jumpGravity;
    float stamina;
    float dashTimer;
    int dashesUsed;
    bool jumpHeld;
    bool jumpAnimationRequested;
    bool dashHeld;
    bool dashRequested;
    bool dashBlockedUntilRelease;
    bool isDashing;

    Vector3 movement;
    CooldownTimer jumpCooldownTimer;
    CooldownTimer attackCooldownTimer;
    PlayerAnimationController animationController;
    StateMachine stateMachine;

    private void Awake() {
        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("PlayerController: Main Camera не найдена. Убедись, что у камеры есть тег MainCamera.", this);
            enabled = false;
            return;
        }

        mainCam = mainCamera.transform;
        freeLookVCam.Follow = transform;
        freeLookVCam.LookAt = transform;
        freeLookVCam.OnTargetObjectWarped(transform, transform.position - freeLookVCam.transform.position - Vector3.forward);

        rb.freezeRotation = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        stamina = maxStamina;
        RecalculateJumpPhysics();
        animationController = new PlayerAnimationController(animator, fallVelocityThreshold);

        // Таймер кулдауна прыжка остаётся в контроллере, а состояние только спрашивает готовность.
        jumpCooldownTimer = new CooldownTimer(jumpCooldown);
        attackCooldownTimer = new CooldownTimer(attackCooldown);

        // Автомат состояний решает, какой набор действий выполнять в текущем кадре.
        stateMachine = new StateMachine();

        var locomotionState = new LocomotionState(this, animator);
        var jumpState = new JumpState(this, animator);
        var dashState = new DashState(this, animator);
        var attackState = new AttackState(this, animator);

        // Прыжок сделан Any-переходом, чтобы его можно было нажать и из обычного движения, и из Dash.
        Any(jumpState, new FuncPredicate(() => CanStartJump));
        Any(dashState, new FuncPredicate(() => CanStartDash));
        At(dashState, locomotionState, new FuncPredicate(() => !ShouldKeepDashing));
        At(jumpState, locomotionState, new FuncPredicate(() => HasFinishedJump));
        At(locomotionState, attackState, new FuncPredicate(() => attackCooldownTimer.IsRunning));
        At(attackState, locomotionState, new FuncPredicate(() => !attackCooldownTimer.IsRunning));
        Any(locomotionState, new FuncPredicate(ReturnToLocomotionState));
        
        stateMachine.SetState(locomotionState);
    }
    bool ReturnToLocomotionState() {
        return groundChecker.IsGrounded 
               && !attackCooldownTimer.IsRunning 
               && !jumpCooldownTimer.IsRunning 
               && !isDashing;
    }
    void At(IState from, IState to, IPredicate condition)=>stateMachine.AddTransition(from, to, condition);
    void Any(IState to, IPredicate condition)=>stateMachine.AddAnyTransition(to, condition);

    public bool CanStartJump => jumpBufferCounter > ZeroF
        && coyoteCounter > ZeroF
        && !jumpCooldownTimer.IsRunning;

    public bool HasFinishedJump => groundChecker.IsGrounded && jumpVelocity <= ZeroF;

    public bool CanStartDash => dashRequested
        && !dashBlockedUntilRelease
        && dashesUsed < maxConsecutiveDashes
        && stamina >= minStaminaToDash;

    public bool ShouldKeepDashing => !dashBlockedUntilRelease
        && stamina > ZeroF
        && (dashTimer > ZeroF || (dashHeld && HasMovementInput));

    public float CurrentStamina => stamina;
    public float StaminaNormalized => maxStamina <= ZeroF ? ZeroF : stamina / maxStamina;

    bool HasMovementInput => movement.sqrMagnitude > 0.0001f;

    private void Start(){
        input.EnablePlayerActions();
    }

    void OnEnable(){
        input.Jump+=OnJump;
        input.Run+=OnRun;
        input.Attack+=OnAttack;
    }

    private void OnDisable() {
        input.Jump-=OnJump;
        input.Run-=OnRun;
        input.Attack-=OnAttack;
    }

    public void Attack(){
        Vector3 attackDirection = transform.forward + transform.forward;
        Collider[] hitColliders = Physics.OverlapSphere(attackDirection, attackRange, LayerMask.GetMask("Enemy"));

        foreach (var hitCollider in hitColliders)
        {
            var enemy = hitCollider.GetComponent<Enemy>();
            if (enemy != null)
            {
                hitCollider.GetComponent<Health>().TakeDamage((int)attackDamage);
            }
        }
    }
    void OnAttack(){
        if (!attackCooldownTimer.IsRunning)
        attackCooldownTimer.Start();
    }

    private void OnJump(bool performed){
        if(performed){
            jumpBufferCounter = jumpBufferTime;
            jumpHeld = true;
        }
        else{
            jumpHeld = false;
        }
    }

    private void OnRun(bool performed){
        dashHeld = performed;

        if(performed){
            dashRequested = true;
        }
        else{
            dashRequested = false;
            dashBlockedUntilRelease = false;
        }
    }

    private void Update() {
        // InputAction Move возвращает Vector2: x = left/right, y = forward/back.
        // Для 3D-движения по XZ переносим y -> z.
        movement = new Vector3(input.Direction.x, 0f, input.Direction.y);
        UpdateAnimator();
        stateMachine.Update();
    }
    private void FixedUpdate() {
        ApplyPlatformTransport();
        jumpCooldownTimer.Tick(Time.fixedDeltaTime);
        UpdateJumpWindows();
        stateMachine.FixedUpdate();
        RegenerateStamina();
        attackCooldownTimer.Tick(Time.fixedDeltaTime);
    }
    public void HandleMovement(float speedMultiplier = 1f){
        var adjustedDirection = GetAdjustedMoveDirection();

        if (adjustedDirection.sqrMagnitude > 0.0001f){
            HandleRotation(adjustedDirection);
            HandleHorizontalMovement(adjustedDirection, speedMultiplier);
            SmoothSpeed(adjustedDirection.magnitude * speedMultiplier);
        }
        else{
            SmoothSpeed(ZeroF);
            rb.linearVelocity = new Vector3(ZeroF, rb.linearVelocity.y, ZeroF);
        }
    }
    public void StartJump(){
        if (!CanStartJump) return;

        jumpBufferCounter = ZeroF;
        coyoteCounter = ZeroF;
        jumpVelocity = jumpInitialVelocity;
        jumpAnimationRequested = true;

        if (jumpCooldown > ZeroF)
        {
            jumpCooldownTimer.Start();
        }
    }

    public void StartDash(){
        if (!CanStartDash) return;

        dashRequested = false;
        isDashing = true;
        dashTimer = dashDuration;
        dashesUsed++;
    }

    public void StopDash(){
        isDashing = false;
        dashTimer = ZeroF;
    }

    public void HandleDashMovement(){
        // Повторное нажатие Shift во время Dash перезапускает короткий импульс, если лимит ещё не исчерпан.
        if (CanStartDash)
        {
            StartDash();
        }

        // Первый короткий участок - сам рывок, дальше скорость снижается до ускоренного бега.
        bool isInitialDash = dashTimer > ZeroF;
        float speedMultiplier = isInitialDash ? dashSpeedMultiplier : sprintSpeedMultiplier;
        dashTimer = Mathf.Max(ZeroF, dashTimer - Time.fixedDeltaTime);

        var adjustedDirection = GetAdjustedMoveDirection();
        if (adjustedDirection.sqrMagnitude <= 0.0001f && isInitialDash)
        {
            adjustedDirection = transform.forward;
        }
        else if (adjustedDirection.sqrMagnitude <= 0.0001f)
        {
            SmoothSpeed(ZeroF);
            rb.linearVelocity = new Vector3(ZeroF, rb.linearVelocity.y, ZeroF);
            return;
        }

        HandleRotation(adjustedDirection);
        HandleHorizontalMovement(adjustedDirection, speedMultiplier);
        SmoothSpeed(speedMultiplier);
    }

    public void DrainDashStamina(){
        stamina = Mathf.Max(ZeroF, stamina - staminaDrainPerSecond * Time.fixedDeltaTime);

        if (stamina <= ZeroF)
        {
            dashBlockedUntilRelease = true;
        }
    }

    public void ApplyDashVerticalMovement(){
        if (groundChecker.IsGrounded)
        {
            ApplyVerticalMovement();
            return;
        }

        // Воздушный Dash должен быть горизонтальным: гасим падение/подъём на время состояния.
        jumpVelocity = ZeroF;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, ZeroF, rb.linearVelocity.z);
    }

    void UpdateAnimator(){
        if (animationController == null) return;

        if (jumpAnimationRequested)
        {
            animationController.TriggerJump();
            jumpAnimationRequested = false;
        }

        animationController.SetFallThreshold(fallVelocityThreshold);
        animationController.Update(currentSpeed, groundChecker.IsGrounded, rb.linearVelocity.y);
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        RecalculateJumpPhysics();
    }



    void UpdateJumpWindows()
    {
        if (jumpBufferCounter > ZeroF)
        {
            jumpBufferCounter -= Time.fixedDeltaTime;
        }

        if (groundChecker.IsGrounded)
        {
            coyoteCounter = coyoteTime;
        }
        else if (coyoteCounter > ZeroF)
        {
            coyoteCounter -= Time.fixedDeltaTime;
        }
    }

    public void ApplyVerticalMovement()
    {
        if (groundChecker.IsGrounded && jumpVelocity <= ZeroF)
        {
            jumpVelocity = ZeroF;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpVelocity, rb.linearVelocity.z);
            return;
        }

        float gravity = jumpGravity;

        if (!jumpHeld && jumpVelocity > ZeroF)
        {
            gravity *= jumpCutGravityMultiplier;
        }
        else if (jumpVelocity < ZeroF)
        {
            gravity *= gravityMultiplier;
        }

        jumpVelocity += gravity * Time.fixedDeltaTime;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpVelocity, rb.linearVelocity.z);
    }

    void RecalculateJumpPhysics()
    {
        float safeDuration = Mathf.Max(0.05f, jumpDuration);
        float safeHeight = Mathf.Max(0.1f, jumpMaxHeight);
        jumpGravity = (-2f * safeHeight) / (safeDuration * safeDuration);
        jumpInitialVelocity = (2f * safeHeight) / safeDuration;
    }

    void RegenerateStamina(){
        // Стамина восстанавливается только вне DashState, чтобы удержание Shift честно расходовало ресурс.
        if (isDashing || stamina >= maxStamina) return;

        stamina = Mathf.Min(maxStamina, stamina + staminaRegenPerSecond * Time.fixedDeltaTime);
    }

    void ApplyPlatformTransport() {
        if (platformCollisionHandler == null || groundChecker == null) return;
        if (!groundChecker.IsGrounded || !platformCollisionHandler.IsOnMovingPlatform) return;
        if (jumpVelocity > ZeroF) return;

        Vector3 platformDelta = platformCollisionHandler.CurrentPlatformDelta;
        if (platformDelta.sqrMagnitude <= 0.0000001f) return;

        rb.MovePosition(rb.position + platformDelta);
    }

    

    private void HandleRotation(Vector3 adjustedDirection){
        var targetRotation = Quaternion.LookRotation(adjustedDirection);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
    }
    Vector3 GetAdjustedMoveDirection(){
        var adjustedDirection = Quaternion.AngleAxis(mainCam.eulerAngles.y, Vector3.up) * movement;
        return Vector3.ClampMagnitude(adjustedDirection, 1f);
    }

    private void HandleHorizontalMovement(Vector3 adjustedDirection, float speedMultiplier = 1f){
        Vector3 targetVelocity = adjustedDirection * moveSpeed * speedMultiplier;
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
    }
    
    private void SmoothSpeed(float value){
        currentSpeed = Mathf.SmoothDamp(currentSpeed, value, ref velocity, smoothTime);
    }


}