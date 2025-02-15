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
    private bool frontisTouchingWall;
    private bool backisTouchingWall;
    public Transform frontwallCheck;
    public Transform backwallCheck;
    public LayerMask wallLayer;
    private float wallJumpCooldown = 0.2f;
    private float wallStickTime = 0.1f;
    private float wallJumpTimer = 0f;

    [Header("Coyote Time Settings")]
    public float wallCoyoteTime = 0.15f;
    private float wallCoyoteCounter;

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
        AdjustGravity();
    }

    private void AdjustGravity() {
        // Increase gravity scale when falling
        if (!isGrounded && rb.velocity.y < 0) {
            rb.gravityScale = 2.0f; // Adjust this value as needed
        } else {
            rb.gravityScale = 1.0f; // Reset to normal gravity
        }
    }

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

        // Jump input starts
        if (Input.GetButtonDown("Jump"))
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= Time.deltaTime;

        // Handle initial jump
        if (jumpBufferCounter > 0 && (coyoteCounter > 0 || wallCoyoteCounter > 0) && !hasJumped) {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            jumpBufferCounter = 0;
            coyoteCounter = 0;
            wallCoyoteCounter = 0;
            hasJumped = true;
            StartCoroutine(HandleContinuedJump());
        }

        // Reduce upward force when jump button is released
        if (Input.GetButtonUp("Jump") && rb.velocity.y > 0) {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * variableJumpMultiplier);
        }
    }

    private IEnumerator HandleContinuedJump() {
        float timeHeld = 0f;
        float maxJumpTime = 0.2f;  // Maximum time the jump force can be applied

        while (Input.GetButton("Jump") && timeHeld < maxJumpTime) {
            if (rb.velocity.y > 0) {  // Only apply force if player is still moving up
                rb.AddForce(Vector2.up * (jumpForce * 0.5f));  // Continue adding upward force
            }
            timeHeld += Time.deltaTime;
            yield return null;
        }
    }

    private void HandleWallInteraction()
    {
        frontisTouchingWall = Physics2D.OverlapCircle(frontwallCheck.position, 0.2f, wallLayer);
        backisTouchingWall = Physics2D.OverlapCircle(backwallCheck.position, 0.2f, wallLayer);

        if ((frontisTouchingWall || backisTouchingWall) && !isGrounded && rb.velocity.y < 0)
        {
            isWallSliding = true;
            isWallJumping = false;
            rb.gravityScale = 0;
            rb.velocity = new Vector2(rb.velocity.x, -wallSlideSpeed);
            animator.SetBool("IsClimb", true);

            wallCoyoteCounter = wallCoyoteTime;
        }
        else
        {
            isWallSliding = false;
            rb.gravityScale = 1;
            animator.SetBool("IsClimb", false);
            wallCoyoteCounter -= Time.deltaTime;
        }

        if (Input.GetButtonDown("Jump") && (isWallSliding || wallCoyoteCounter > 0))
        {
            WallJump(frontisTouchingWall);
        }

        if (wallJumpTimer > 0)
        {
            wallJumpTimer -= Time.deltaTime;
        }
    }

    private void WallJump(bool isFrontWall)
    {
        float jumpDirection = isFrontWall ? (isFacingRight ? -1 : 1) : (isFacingRight ? 1 : -1);

        rb.velocity = Vector2.zero;
        rb.AddForce(new Vector2(wallJumpForceX * jumpDirection, wallJumpForceY), ForceMode2D.Impulse);

        if (isFrontWall != isFacingRight)
        {
            Flip();
        }

        isWallJumping = true;
        wallJumpTimer = wallJumpCooldown;
        coyoteCounter = coyoteTime;
        wallCoyoteCounter = 0;

        StartCoroutine(DisableWallStick());
    }

    private void HandleFlip()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");

        if ((moveInput > 0 && !isFacingRight) || (moveInput < 0 && isFacingRight))
        {
            Flip();
        }
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
    }

    private void HandleDash()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash && !hasDashed && !isWallSliding)
        {
            StartCoroutine(Dash());
        }
    }

    private IEnumerator Dash()
    {
        isDashing = true;
        canDash = false;
        rb.gravityScale = 0;
        float dashDirection = isFacingRight ? 1 : -1;
        rb.velocity = new Vector2(dashDirection * dashSpeed, rb.velocity.y);

        yield return new WaitForSeconds(dashTime);

        rb.gravityScale = 1;
        isDashing = false;

        yield return new WaitForSeconds(0.1f); // Short cooldown after dashing
        canDash = true;
    }

    private bool CheckIfGrounded()
    {
        RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, groundLayer);
        return hit.collider != null && rb.velocity.y <= 0.1f;
    }

    private IEnumerator DisableWallStick()
    {
        isWallSliding = false;
        yield return new WaitForSeconds(wallStickTime);
        isWallJumping = false;
    }
    private void HandleAnimations() {
        animator.SetBool("isRunning", Mathf.Abs(rb.velocity.x) > 0.1f);
        animator.SetBool("isGrounded", isGrounded);
        animator.SetBool("isJumping", rb.velocity.y > 0);
        
        // Here we ensure the falling animation is played when the player is descending
        bool currentlyFalling = !isGrounded && rb.velocity.y < 0;
        animator.SetBool("isFalling", currentlyFalling);

        animator.SetBool("isWallSliding", isWallSliding);
        animator.SetBool("isDashing", isDashing);

        if (isWallSliding) {
            animator.SetBool("isClimbing", true);
        } else {
            animator.SetBool("isClimbing", false);
        }

        // Use this logic to handle jump and fall transitions smoothly
        if (!isGrounded && rb.velocity.y < 0) {
            animator.SetBool("isJumping", false);
            animator.SetBool("isFalling", true);
        } else if (rb.velocity.y > 0) {
            animator.SetBool("isJumping", true);
            animator.SetBool("isFalling", false);
        }

        if (isGrounded) {
            animator.SetBool("isJumping", false);
            animator.SetBool("isFalling", false);
        }
}


}
