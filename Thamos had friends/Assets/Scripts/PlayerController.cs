﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerController : MonoBehaviour {

    protected string characterName;

    protected abstract string giveCharacterName();
    
    [SerializeField] [Range(1, 20)] protected float speed = 10f;
    [SerializeField] [Range(1, 20)] protected float jumpForce = 10f;
    [SerializeField] [Range(0, 5)] protected int extraJumps = 0;
    [SerializeField] [Range(0.1f, 0.3f)] protected float jumpTime = 0.2f;
    [SerializeField] [Range(0.1f, 1f)] protected float moveToGoalSpeed = 0.7f;
    [SerializeField] protected Transform groundCheckTopLeft;
    [SerializeField] protected Transform groundCheckBottomRight;
    [SerializeField] protected LayerMask layerMask;
    [SerializeField] protected GameObject goal;
    [SerializeField] protected GameObject focusIndicator;


    protected Rigidbody2D rb;
    protected Animation playerAnimation;
    protected SpriteRenderer spriteRenderer;
    protected Color initialColor;
    
    protected float horizontalMovement;

    protected bool onGoal = false;
    protected bool landingPlayed = false;
    protected bool isOnFocus = false;
    protected bool isGrounded = false;
    protected bool movable = true;

    protected bool jumping = false;
    protected int extraJumpsAvailable;
    protected float jumpTimeCounter;

    protected void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        playerAnimation = GetComponent<Animation>();
        initialColor = spriteRenderer.color;
        characterName = giveCharacterName();
        extraJumpsAvailable = extraJumps;
        jumpTimeCounter = jumpTime;

        //set first in the list onFocus
        transform.parent.GetChild(0).GetComponent<PlayerController>().setOnFocus();
    }

    protected void Update()
    {
        isGrounded = Physics2D.OverlapArea(groundCheckTopLeft.position, groundCheckBottomRight.position, layerMask);

        if (movable)
        {
            horizontalMovement = Input.GetAxis("Horizontal");
            jumpManagement();
            changeCharacterManagement();
        } else
        {
            horizontalMovement = 0;
        }

        if (isGrounded)
        {
            playLanding();
            extraJumpsAvailable = extraJumps;
        } else
        {
            landingPlayed = false;
        }

        if (!isOnFocus)
        {
            focusIndicator.SetActive(false);
            movable = false;
        } else
        {
            focusIndicator.SetActive(true);
            movable = true;
        }
    }

    protected void FixedUpdate()
    {
       rb.velocity = new Vector2(horizontalMovement * speed, rb.velocity.y);
    }

    protected void changeCharacterManagement()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            isOnFocus = false;
            int actualFocus = transform.GetSiblingIndex();
            transform.parent.GetChild((actualFocus + 1) % transform.parent.childCount).GetComponent<PlayerController>().setOnFocus();
        }
    }

    protected void jumpManagement()
    {
        if (Input.GetButton("Jump"))
        {
            if (isGrounded)
            {
                jumping = true;
                if (Input.GetButtonDown("Jump"))
                    playerAnimation.Play(characterName + "Jump");
            }
            else if (extraJumpsAvailable > 0)
            {
                if (Input.GetButtonDown("Jump"))
                {
                    jumping = true;
                    playerAnimation.Stop(characterName + "Jump"); // si double saut rapide
                    playerAnimation.Play(characterName + "Jump");
                    extraJumpsAvailable--;
                }
            }

            if(jumping && jumpTimeCounter > 0)
            {
                rb.velocity = new Vector2(rb.velocity.x, 1 * jumpForce);
                jumpTimeCounter -= Time.deltaTime;
            }
        }
        else if (Input.GetButtonUp("Jump"))
        {
            jumpTimeCounter = jumpTime;
            jumping = false;
        }
    }

    protected void playLanding() // voir les onLandEvent...
    {
        if (!landingPlayed && !onGoal)
        {
            playerAnimation.Play(characterName + "Landing");
            landingPlayed = true;
        }
    }

    protected void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject == goal)
        {
            spriteRenderer.color = new Color(1, 1, 1, 1);
            onGoal = true;
            if (checkIfAllOnGoal())
            {
                movable = false;
                rb.velocity = Vector2.zero;
                rb.gravityScale = 0;
                focusIndicator.SetActive(false);
                StartCoroutine(moveToGoal(transform, goal.transform.position, moveToGoalSpeed));
            }
        }
    }

    protected void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject == goal && !checkIfAllOnGoal())
        {
            spriteRenderer.color = initialColor;
        }
    }

    protected bool checkIfAllOnGoal()
    {
        foreach (Transform character in transform.parent)
        {
            PlayerController characterScript = character.GetComponent<PlayerController>();
            if (!characterScript.isOnGoal())
            {
                return false;
            }
        }
        return true;
    }

    protected bool isOnGoal()
    {
        return onGoal;
    }

    protected void setOnFocus()
    {
        isOnFocus = true;
    }

    protected IEnumerator moveToGoal(Transform toMove, Vector2 destination, float speed)
    {
        while((Vector2) toMove.position != destination)
        {
            Vector3 newPosition = Vector3.MoveTowards(toMove.position, destination, speed * Time.deltaTime);
            newPosition.z = 0;
            toMove.position = newPosition;
            yield return null;
        }
        Destroy(goal);
        playerAnimation.Play(characterName + "Goal");
    }
}
