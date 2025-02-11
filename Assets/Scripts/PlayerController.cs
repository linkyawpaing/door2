using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;
    private bool isGrounded;
    private bool isWallSliding;
    private bool isDashing;
    private bool hasJumped;

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
    public float wallJumpForceX = 6f;
    public float wallJumpForceY = 12f;
    private bool isTouchingWall;
    public Transform wallCheck;
    public LayerMask wallLayer;

    [Header("Dash Settings")]
    public float dashSpeed = 16f;
    public float dashTime = 0.15f;
    private bool canDash = true;
    private bool hasDashed;

    [Header("Ground Detection")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.2f; // ✅ Adjust ground detection distance

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");

        // 🟢 Fix Ground Detection Issue using Raycast
        isGrounded = CheckIfGrounded();

        // 🟢 Coyote Time: Allows forgiving jumps
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

        // 🟢 Jump Buffer: Stores jump input before landing
        if (Input.GetButtonDown("Jump"))
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= Time.deltaTime;

        // 🟢 Jump (Only if grounded or in coyote time)
        if (jumpBufferCounter > 0 && coyoteCounter > 0 && !hasJumped)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            jumpBufferCounter = 0;
            coyoteCounter = 0;
            hasJumped = true;
        }

        // 🟢 Variable Jump Height (Hold for higher jump)
        if (Input.GetButtonUp("Jump") && rb.velocity.y > 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * variableJumpMultiplier);
        }

        // 🟢 Wall Sliding
        isTouchingWall = Physics2D.OverlapCircle(wallCheck.position, 0.2f, wallLayer);
        if (isTouchingWall && !isGrounded && rb.velocity.y < 0)
        {
            isWallSliding = true;
            rb.velocity = new Vector2(rb.velocity.x, -wallSlideSpeed);
        }
        else
        {
            isWallSliding = false;
        }

        // 🟢 Wall Jump (Prevents mid-air double jumping)
        if (Input.GetButtonDown("Jump") && isWallSliding)
        {
            rb.velocity = new Vector2(-moveInput * wallJumpForceX, wallJumpForceY);
            hasJumped = false;
            coyoteCounter = 0;
        }

        // 🟢 Dash (Limited to once per air time)
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash && !hasDashed)
        {
            StartCoroutine(Dash(moveInput));
            hasDashed = true;
        }

        // 🟢 Apply Movement
        float targetSpeed = moveInput * moveSpeed;
        float speedDifference = targetSpeed - rb.velocity.x;
        float accelerationRate = isGrounded ? acceleration : acceleration * airControl;
        float movement = speedDifference * accelerationRate * Time.deltaTime;

        rb.velocity = new Vector2(rb.velocity.x + movement, rb.velocity.y);

        // 🟢 Apply Ground Friction (Prevents Sudden Stops)
        if (moveInput == 0 && isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x * groundFriction, rb.velocity.y);
        }
    }

    private IEnumerator Dash(float moveInput)
    {
        isDashing = true;
        canDash = false;
        rb.velocity = new Vector2(moveInput * dashSpeed, 0);
        yield return new WaitForSeconds(dashTime);
        isDashing = false;
        yield return new WaitForSeconds(0.5f);
        canDash = true;
    }

    private bool CheckIfGrounded()
    {
        // 🟢 Raycast Ground Check - More Accurate than OverlapCircle
        RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, groundLayer);

        // ✅ Draw Debug Line in Scene View
        Debug.DrawRay(groundCheck.position, Vector2.down * groundCheckDistance, hit.collider != null ? Color.green : Color.red);

        return hit.collider != null && rb.velocity.y <= 0.1f;
    }
}
