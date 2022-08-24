using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("PlayerSettings")] [SerializeField]
    private float speed = 5f;

    [SerializeField] private float jumpSpeed = 8f;

    [Header("PlayerComponents")] [SerializeField]
    private Rigidbody2D player;

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private BoxCollider2D groundCheckerCollider;
    [SerializeField] private Animator animator;

    [Header("LayerMaskSettings")] [SerializeField]
    private LayerMask groundLayerMask;

    private float _direction;

    private bool _isTouchingGround;


    private void Update()
    {
        _isTouchingGround = Physics2D.OverlapBox(groundCheckerCollider.transform.position, groundCheckerCollider.size,
            0f, groundLayerMask);

        _direction = Input.GetAxis("Horizontal");

        if (_direction != 0f)
        {
            animator.SetBool("isWalking", true);
        }
        else
        {
            animator.SetBool("isWalking",false);
        }

        PlayerInputMovementDirection();

        PlayerJumpHandle();

        if (Input.GetKeyDown(KeyCode.E))
        {
        }
    }

    private void PlayerJumpHandle()
    {
        if (Input.GetButtonDown("Jump") && _isTouchingGround)
        {
            player.velocity = new Vector2(player.velocity.x, jumpSpeed);
        }
    }

    private void PlayerInputMovementDirection()
    {
        if (_direction > 0f)
        {
            spriteRenderer.flipX = false;
            player.velocity = new Vector2(_direction * speed, player.velocity.y);
        }
        else if (_direction < 0f)
        {
            spriteRenderer.flipX = true;
            player.velocity = new Vector2(_direction * speed, player.velocity.y);
        }
        else
        {
            player.velocity = new Vector2(0, player.velocity.y);
        }
    }
}