using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour {

    private Animator animator;
    private int horizontalHash;
    private int verticalHash;
    private int attackHash;

	// Use this for initialization
	void Start () {
        animator = GetComponent<Animator>();

        // Calculate the integer hash for the animation strings.
        // This is a performace tweak to stop it happening each frame.
        horizontalHash = Animator.StringToHash("Horizontal");
        verticalHash = Animator.StringToHash("Vertical"); ;
        attackHash = Animator.StringToHash("Attack");
	}
	
	// Update is called once per frame
	void Update () {
        float xAxis = Input.GetAxis("Horizontal") * 2.32f;
        float yAxis = Input.GetAxis("Vertical") * 5.66f;

        if (Input.GetMouseButtonDown(0))
        {
            animator.SetTrigger(attackHash);
        }

        animator.SetFloat(horizontalHash, xAxis, 0.2f, Time.deltaTime);
        animator.SetFloat(verticalHash, yAxis, 1.0f, Time.deltaTime);
	}
}
