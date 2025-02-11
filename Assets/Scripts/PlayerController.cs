using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator animator;
    private bool isGrounded;
    private bool isWallSliding;
    private bool isWallJumping;
    private bool isDashing;
    private bool hasJumped;
    private bool isFacingRight = true;

    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float acceleration = 10f;
    public float deceleration = 6f;
    public float airControl = 0.8f;
    public float groundFriction = 0.8f;

    [Header("Jump Settings")]
    public float jumpForce = 14f;
    public float variableJumpMultiplier = 0.5f;
    public float coyoteTime = 0.15f;
    private float coyoteCounter;
    public float jumpBufferTime = 0.2f;
    private float jumpBufferCounter;

    [Header("Wall Slide & Jump")]
    public float wallSlideSpeed = 2f;
    public float wallJumpForceX = 8f;
    public float wallJumpForceY = 14f;
    private bool isTouchingWall;
    public Transform wallCheck;
    public LayerMask wallLayer;
    private float wallJumpCooldown = 0.2f;
    private float wallStickTime = 0.1f;
    private float wallJumpTimer = 0f;

    [Header("Dash Settings")]
    public float dashSpeed = 12f;
    public float dashTime = 0.15f;
    public float dashEndDeceleration = 0.1f;
    private bool canDash = true;
    private bool hasDashed;

    [Header("Ground Detection")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.3f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>(); 
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void Update()
    {
        HandleMovement();
        HandleJump();
        HandleWallInteraction();
        HandleFlip();
        HandleDash();
        HandleAnimations();
    }

    // ✅ Handles Movement
    private void HandleMovement()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");

        if (!isDashing && !isWallJumping)
        {
            float targetSpeed = moveInput * moveSpeed;
            float speedDifference = targetSpeed - rb.velocity.x;
            float accelerationRate = isGrounded ? acceleration : acceleration * airControl;
            float movement = speedDifference * accelerationRate * Time.deltaTime;

            rb.velocity = new Vector2(rb.velocity.x + movement, rb.velocity.y);
        }

        if (moveInput == 0 && isGrounded && !isDashing)
        {
            rb.velocity = new Vector2(rb.velocity.x * groundFriction, rb.velocity.y);
        }
    }

    // ✅ Handles Jump Logic
    private void HandleJump()
    {
        isGrounded = CheckIfGrounded();

        if (isGrounded)
        {
            coyoteCounter = coyoteTime;
            hasJumped = false;
            hasDashed = false;
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }

        if (Input.GetButtonDown("Jump"))
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= Time.deltaTime;

        if (jumpBufferCounter > 0 && coyoteCounter > 0 && !hasJumped)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            jumpBufferCounter = 0;
            coyoteCounter = 0;
            hasJumped = true;
        }

        if (Input.GetButtonUp("Jump") && rb.velocity.y > 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * variableJumpMultiplier);
        }
    }

    // ✅ Handles Wall Sliding & Wall Jumping
    private void HandleWallInteraction()
    {
        isTouchingWall = Physics2D.OverlapCircle(wallCheck.position, 0.2f, wallLayer);

        if (isTouchingWall && !isGrounded && rb.velocity.y < 0)
        {
            isWallSliding = true;
            isWallJumping = false;
            rb.gravityScale = 0;
            rb.velocity = new Vector2(rb.velocity.x, -wallSlideSpeed);
            animator.SetBool("IsClimb", true);
        }
        else
        {
            isWallSliding = false;
            rb.gravityScale = 1;
            animator.SetBool("IsClimb", false);
        }

        if (isWallSliding && Input.GetButtonDown("Jump"))
        {
            WallJump();
        }

        if (wallJumpTimer > 0)
        {
            wallJumpTimer -= Time.deltaTime;
        }
    }

    private void WallJump()
    {
        float jumpDirection = isFacingRight ? -1 : 1;
        isWallJumping = true;
        wallJumpTimer = wallJumpCooldown;
        rb.velocity = Vector2.zero;
        rb.AddForce(new Vector2(wallJumpForceX * jumpDirection, wallJumpForceY), ForceMode2D.Impulse);
        StartCoroutine(DisableWallStick());
    }

    private IEnumerator DisableWallStick()
    {
        isWallSliding = false;
        yield return new WaitForSeconds(wallStickTime);
        isWallJumping = false;
    }

    // ✅ Handles Flip Logic
    private void HandleFlip()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");

        if ((moveInput > 0 && !isFacingRight) || (moveInput < 0 && isFacingRight))
        {
            isFacingRight = !isFacingRight;
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
        }
    }

    // ✅ Handles Dash
    private void HandleDash()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash && !hasDashed)
        {
            StartCoroutine(Dash());
            hasDashed = true;
        }
    }

    private IEnumerator Dash()
    {
        isDashing = true;
        canDash = false;
        rb.gravityScale = 0;
        float dashDirection = isFacingRight ? 1 : -1;
        rb.velocity = new Vector2(dashDirection * dashSpeed, 0);

        yield return new WaitForSeconds(dashTime);

        rb.gravityScale = 1;
        isDashing = false;

        yield return new WaitForSeconds(0.5f);
        canDash = true;
    }

    // ✅ Handles Animations
    private void HandleAnimations()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");

        animator.SetBool("IsRunning", moveInput != 0);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsFalling", rb.velocity.y < 0 && !isGrounded);
        animator.SetBool("IsWallSliding", isWallSliding);
        animator.SetBool("IsDashing", isDashing);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            animator.SetTrigger("Jump");
        }
    }

    // ✅ Ground Detection
    private bool CheckIfGrounded()
    {
        RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, groundLayer);
        Debug.DrawRay(groundCheck.position, Vector2.down * groundCheckDistance, hit.collider != null ? Color.green : Color.red);
        return hit.collider != null && rb.velocity.y <= 0.1f;
    }
}
