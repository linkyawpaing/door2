using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; // Ensure this is the input system you're using

public class PlayerShooting : MonoBehaviour
{
    public Transform Player;
    private Rigidbody2D rb;
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

    public Transform Stick;

    public PlayerController pc;

    public float maxShootingAngle = 75f;

    public Transform Right;

    void Awake()
    {
        rb = Player.GetComponent<Rigidbody2D>();
        
    }

    void Update()
    {
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }

        HandleInput();
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
            Vector2 plydir = dragEndPos - Player.transform.position;

            Vector3 shootingDirection = dragEndPos - shootPoint.position;
            shootingDirection.z = 0; // Ensure it's purely in 2D
            float angle = Vector3.Angle(shootPoint.right, shootingDirection.normalized);

            // UpdateTrajectory(shootPoint.position, dragEndPos);


            // Debug.Log("facingRight is+ "+ facingRight);
            Debug.Log("Player AngleRight + "+ Vector3.Angle(Player.transform.right, dragEndPos));
            Debug.Log("Player AngleUP + "+ Vector3.Angle(Player.transform.up, dragEndPos));
            Debug.Log("Player AngleDown - "+ Vector3.Angle(-Player.transform.up, dragEndPos));
            Debug.Log("ShootPoint AngleRight + "+ Vector3.Angle(shootPoint.right, shootingDirection.normalized));
            Debug.Log("ShootPoint AngleUP + "+ Vector3.Angle(shootPoint.up , dragEndPos));
            Debug.Log("ShootPoint AngleDown - "+ Vector3.Angle(-shootPoint.up,dragEndPos));
            if (Vector3.Angle(shootPoint.up , shootingDirection.normalized) > 20f && Vector3.Angle(-shootPoint.up,shootingDirection.normalized) > 60f)
            {
                if (Vector3.Angle(shootPoint.right, shootingDirection.normalized) > 90 && pc.isFacingRight)
                {
                    Stick.transform.position = Right.position;
                    pc.Flip();
                }
                else if (Vector3.Angle(shootPoint.right, shootingDirection.normalized) < 90 && (!pc.isFacingRight))
                {
                    Stick.transform.position = Right.position;
                    pc.Flip();
                }
            }
            Stick.transform.right = plydir;
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


    private void Shoot(Vector3 dragEndPos)
    {
        Vector3 shootingDirection = dragEndPos - shootPoint.position;
        shootingDirection.z = 0; // Ensure it's purely in 2D

        float angle = Vector3.Angle(shootPoint.right, shootingDirection.normalized);
        // if (angle > maxShootingAngle)
        // {
        //     angle = maxShootingAngle;
        // }
        Debug.Log("Angle is "+ angle);
        if (angle <= maxShootingAngle)
        {
            Vector2 force = shootingDirection.normalized * shootingPower;
            GameObject projectile = Instantiate(projectilePrefab, shootPoint.position, shootPoint.rotation);
            projectile.GetComponent<Rigidbody2D>().AddForce(force, ForceMode2D.Impulse);
            pc.Recoil(pc.isFacingRight);
        }
        else
        {
            Vector2 force = shootingDirection.normalized * shootingPower;
            GameObject projectile = Instantiate(projectilePrefab, shootPoint.position, shootPoint.rotation);
            projectile.GetComponent<Rigidbody2D>().AddForce(force, ForceMode2D.Impulse);
            pc.Recoil(pc.isFacingRight);
        }


    }

    private void UpdateTrajectory(Vector3 start, Vector3 end)
    {
        Vector2 start2D = new Vector2(start.x, start.y);
        Vector2 end2D = new Vector2(end.x, end.y);
        Vector2 direction = end2D - start2D;
        Vector3[] trajectoryPoints = new Vector3[30];
        trajectoryLineRenderer.positionCount = trajectoryPoints.Length;
        float stepSize = 1.0f / trajectoryPoints.Length;

        for (int i = 0; i < trajectoryPoints.Length; i++)
        {
            float t = stepSize * i;
            Vector2 point2D = start2D + direction * t + 0.1f * Physics2D.gravity * (t * t);
            trajectoryPoints[i] = new Vector3(point2D.x, point2D.y, 0);
        }

        trajectoryLineRenderer.SetPositions(trajectoryPoints);
    }
}
