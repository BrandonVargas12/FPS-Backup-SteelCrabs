using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class enemyAI : MonoBehaviour, IDamageable
{
    [Header("----- Components -----")]
    [SerializeField] NavMeshAgent agent;
    [SerializeField] Renderer rend;
    [SerializeField] Animator anim;

    [Header("----- Enemy Stats -----")]
    [Range(1, 10)] [SerializeField] int hP;
    [Range(1, 10)] [SerializeField] float speedChase;
    [Range(1, 10)] [SerializeField] int playerFaceSpeed;
    [Range(1, 50)] [SerializeField] int roamRadius;
    [Range(1, 180)] [SerializeField] int viewAngle;

    [Header("----- Weapon Stats -----")]
    [SerializeField] float fireRate;
    [SerializeField] GameObject bullet;
    [SerializeField] Transform bulletPos;
    
    Vector3 playerDir;
    bool isShooting;
    bool takingDmg;
    public bool playerInRange;
    Vector3 lastPlayerPos;
    float stoppingDistanceOrig;
    float speedOrig;
    Vector3 startingPos;
    bool roamPathValid;
    float angle;

    // Start is called before the first frame update
    void Start()
    {
        lastPlayerPos = transform.position;
        stoppingDistanceOrig = agent.stoppingDistance;
        speedOrig = agent.speed;
        startingPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (agent.enabled)
        {
            angle = Vector3.Angle(playerDir, transform.forward);
            playerDir = gameManager.instance.player.transform.position - transform.position;

            anim.SetFloat("Speed", Mathf.Lerp(anim.GetFloat("Speed"), agent.velocity.normalized.magnitude, Time.deltaTime * 4));
            if (!takingDmg)
            {
                if (playerInRange)
                {
                    canSeePlayer();

                }
                if (agent.remainingDistance < 0.1f && agent.destination != gameManager.instance.player.transform.position)
                {
                    roam();
                }
            }
        }
    }
    void roam()
    {
        agent.stoppingDistance = 0;
        agent.speed = speedOrig;

        Vector3 randomDir = Random.insideUnitSphere * roamRadius;
        randomDir += startingPos;

        NavMeshHit hit;
        NavMesh.SamplePosition(randomDir, out hit, 1, 1);
        NavMeshPath path = new NavMeshPath();

        agent.CalculatePath(hit.position, path);
        agent.SetPath(path);

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;

        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            lastPlayerPos = gameManager.instance.player.transform.position;
            agent.stoppingDistance = 0;
        }
    }

    void FacePlayer()
    {
        playerDir.y = 0;
        Quaternion rotation = Quaternion.LookRotation(playerDir);
        transform.rotation = Quaternion.Lerp(transform.rotation, rotation,Time.deltaTime * playerFaceSpeed);
    }

    void canSeePlayer()
    {
        float angle = Vector3.Angle(playerDir, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(transform.position + transform.up * 1.5f, playerDir, out hit))
        {
            Debug.DrawRay(transform.position + transform.up, playerDir);
            if (hit.collider.CompareTag("Player") && angle <= viewAngle)
            {
                agent.SetDestination(gameManager.instance.player.transform.position);
                agent.stoppingDistance = stoppingDistanceOrig;

                FacePlayer();

                if (!isShooting)
                {
                    StartCoroutine(Shoot());
                }
            }
            else
            {
                agent.stoppingDistance = 0;
            }
            if (gameManager.instance.playerDeadMenu.activeSelf)
            {
                playerInRange = false;
                agent.stoppingDistance = 0;
            }
        }
    }

    public void TakeDamage(int dmg)
    {
        hP -= dmg;
        anim.SetTrigger("Damage");

        lastPlayerPos = gameManager.instance.player.transform.position;
        agent.stoppingDistance = 0;

        StartCoroutine(FlashColor());

        if (hP <= 0 && agent.enabled)
        {
            enemyDead();
        }
    }

    IEnumerator FlashColor()
    {
        takingDmg = true;
        agent.speed = 0;
        rend.material.color = Color.red;
        yield return new WaitForSeconds(0.50f);
        rend.material.color = Color.white;
        agent.speed = speedOrig;
        takingDmg = false;
    }

    IEnumerator Shoot()
    {
        isShooting = true;

        Instantiate(bullet, bulletPos.position, transform.rotation);
        
        yield return new WaitForSeconds(fireRate);
        isShooting = false;
    }
    void enemyDead()
    {
        gameManager.instance.EnemyDecrement();
        anim.SetBool("Dead", true);
        agent.enabled = false;
        foreach (Collider col in GetComponents<Collider>())
            col.enabled = false;
    }
}
