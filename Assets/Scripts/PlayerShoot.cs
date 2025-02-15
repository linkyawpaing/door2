using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; // Ensure this is the input system you're using

public class PlayerShooting : MonoBehaviour
{
    public Transform Player;
    private Rigidbody2D rb;
    public PlayerController pc;
    SpriteRenderer psp; 

    public Animator ani;
    public Transform shootPoint;
    public GameObject projectilePrefab;
    public LineRenderer trajectoryLineRenderer;
    public float shootingPower = 10f;
    public float cooldown = 1f;
    private float cooldownTimer;
    private Vector2 dragStartPos;

    private bool facingRight;

    void Update()
    {
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }

        HandleInput();
    }
    void Start()
    {
        rb = Player.GetComponent<Rigidbody2D>();
        ani = Player.GetComponent<Animator>();
        pc = Player.GetComponent<PlayerController>();
        
    }

    private void HandleInput()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector3 screenPosition = Mouse.current.position.ReadValue();
            screenPosition.z = 7; // Distance from the camera
            Vector3 dragStartPos = Camera.main.ScreenToWorldPoint(screenPosition);
            dragStartPos.z = 0; // Ensure it's purely in 2D
        }

        if (Mouse.current.leftButton.isPressed)
        {
            psp = Player.GetComponent<SpriteRenderer>();
            Vector3 screenPosition = Mouse.current.position.ReadValue();
            screenPosition.z = 7;
            Vector3 dragEndPos = Camera.main.ScreenToWorldPoint(screenPosition);
            dragEndPos.z = 0;
            UpdateTrajectory(shootPoint.position, dragEndPos);
            if (Player.localScale.x == -1)
            {
                facingRight = false;
            }
            if (Player.localScale.x == 1)
            {
                facingRight = true;
            }
            if (Vector3.Angle(Player.transform.up , dragEndPos) > 20f && Vector3.Angle(-Player.transform.up,dragEndPos) > 60f)
            {
                if (Vector3.Angle(Player.transform.right, dragEndPos) > 90 && facingRight)
                {
                    pc.Flip();
                }
                else if (Vector3.Angle(Player.transform.right, dragEndPos) < 90 && (!facingRight))
                {
                    pc.Flip();
                }
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && cooldownTimer <= 0)
        {
            Vector3 screenPosition = Mouse.current.position.ReadValue();
            screenPosition.z = 7;
            Vector3 dragEndPos = Camera.main.ScreenToWorldPoint(screenPosition);
            dragEndPos.z = 0;
            Shoot(dragEndPos);
            cooldownTimer = cooldown;
            trajectoryLineRenderer.positionCount = 0; // Clear the line
        }
    }


    private void Shoot(Vector2 dragEndPos)
    {
        Vector2 direction = dragEndPos - (Vector2)shootPoint.position;
        GameObject projectile = Instantiate(projectilePrefab, shootPoint.position, Quaternion.identity);
        projectile.GetComponent<Rigidbody2D>().AddForce(direction.normalized * shootingPower, ForceMode2D.Impulse);
    }

    private void UpdateTrajectory(Vector2 start, Vector2 end)
    {
        Vector2 direction = end - start;
        Vector3[] trajectoryPoints = new Vector3[30];
        trajectoryLineRenderer.positionCount = trajectoryPoints.Length;
        float stepSize = 1.0f / trajectoryPoints.Length;

        for (int i = 0; i < trajectoryPoints.Length; i++)
        {
            float t = stepSize * i;
            trajectoryPoints[i] = start + direction * t + 0.5f * Physics2D.gravity * (t * t) * new Vector2(1, 1);
        }

        trajectoryLineRenderer.SetPositions(trajectoryPoints);
    }
}
