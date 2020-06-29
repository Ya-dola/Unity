﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using TreeEditor;
using UnityEngine;

public class BallController : MonoBehaviour
{
    public WinMenu winMenu;

    [Header("Players")]
    public PlayerController leftPlayer;
    public PlayerController rightPlayer;

    [Header("Ball Settings")]
    public ParticleSystem goalParticle;
    public ParticleSystem hitParticle;
    public float startSpeed;
    public float maxSpeedSqr;
    [Range(1f, 1.2f)] public float acceleration;
    [Range(1f, 10f)] public float shrinkSpeed;

    private Vector3 initSize;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Vector2 prevVel;


    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        StartCoroutine(KickOff());
        initSize = transform.localScale;
        prevVel = Vector2.zero;
    }

    public IEnumerator KickOff()
    {
        prevVel = Vector2.zero;
        int x = 1;
        if ((int)UnityEngine.Random.Range(0,2) == 0)
            x = -1;
        yield return new WaitForSeconds(0.5f);
        rb.velocity = new Vector2(x*startSpeed, 0);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        PlayHitParticleAccordingToCollission(collision);
        if (collision.collider.tag == "raquette")
        {
            CalculateNewTrajectory(collision);
        }
        else if (collision.collider.tag == "but")
        {
            rb.velocity = Vector2.zero;
            StartCoroutine("Goal");
        }
    }

    private void PlayHitParticleAccordingToCollission(Collision2D collision)
    {
        ParticleSystem particle = Instantiate(hitParticle);
        Vector2 contactPos = collision.GetContact(0).point;
        particle.transform.position = contactPos;

        float xMin = contactPos.x - 0.2f;
        float xMax = contactPos.x + 0.2f;

        if (transform.position.x < xMax && transform.position.x > xMin) // collision sur mur haut ou bas
        {
            if (transform.position.y < contactPos.y)
                particle.transform.localRotation = Quaternion.Euler(-270, 0, 0);
            else if (transform.position.y > contactPos.y)
                particle.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        }
        else
        {
            if (transform.position.x > contactPos.x)
                particle.transform.localRotation = Quaternion.Euler(0, 90, 0);
            if (transform.position.x < contactPos.x)
                particle.transform.localRotation = Quaternion.Euler(0, 270, 0);
        }
        particle.Play();
    }

    private void CalculateNewTrajectory(Collision2D collision)
    {
        float nextMagnitudeSqr = 0;

        if (prevVel.sqrMagnitude > rb.velocity.sqrMagnitude) // si mauvaise collision (coin de raquette)
            nextMagnitudeSqr = (prevVel * acceleration).sqrMagnitude;
        else
            nextMagnitudeSqr = (rb.velocity * acceleration).sqrMagnitude;

        if (nextMagnitudeSqr > maxSpeedSqr)
        {
            nextMagnitudeSqr = maxSpeedSqr;
        }
            

        float tranche = collision.collider.transform.localScale.y / 8;
        float segment = Mathf.RoundToInt((transform.position.y - collision.collider.transform.position.y) / tranche);

        int xDirection = 1;
        int yDirection = 1;
        if (segment < 0)
            yDirection = -1;
        if (transform.position.x > 0)
            xDirection = -1;

        float angle = 0;
        if (Mathf.Abs(segment) == 1)
            angle = 15;
        else if (Mathf.Abs(segment) == 2)
            angle = 30;
        else if (Mathf.Abs(segment) >= 3)
            angle = 45;

        float xVel = xDirection * Mathf.Cos(angle * Mathf.Deg2Rad) * Mathf.Sqrt(nextMagnitudeSqr);
        float yVel = yDirection * Mathf.Sin(angle * Mathf.Deg2Rad) * Mathf.Sqrt(nextMagnitudeSqr);

        rb.velocity = new Vector2(xVel, yVel);
        prevVel = new Vector2(xVel, yVel);
    }

    private IEnumerator Goal()
    {
        yield return new WaitForSeconds(0.5f);
        while (transform.localScale != Vector3.zero)
        {
            transform.localScale = Vector2.MoveTowards(transform.localScale, Vector3.zero, shrinkSpeed * Time.deltaTime);
            yield return null;
        }
        yield return new WaitForSeconds(0.25f);
        ParticleSystem goalP = Instantiate(goalParticle);
        goalP.transform.position = transform.position;
        goalP.Play();
        yield return new WaitForSeconds(1f);

        if (transform.position.x > 0)
            leftPlayer.Goal();
        else if (transform.position.x < 0)
            rightPlayer.Goal();

        yield return new WaitForSeconds(3f); // Delai a voir selon coroutine displayscore
        // Verifier si partie n'est pas finie
        if (leftPlayer.points == 5 || rightPlayer.points == 5)
        {
            StartCoroutine("EndGame");
            yield break;
        }
        transform.position = Vector2.zero;
        while (transform.localScale != initSize)
        {
            transform.localScale = Vector3.MoveTowards(transform.localScale, initSize, shrinkSpeed * Time.deltaTime);
            yield return null;
        }
        //yield return new WaitForSeconds(0.5f);
        StartCoroutine(KickOff());
    }

    private IEnumerator EndGame()
    {
        yield return new WaitForSeconds(1f);
        winMenu.ActivateMenu();
    }

}