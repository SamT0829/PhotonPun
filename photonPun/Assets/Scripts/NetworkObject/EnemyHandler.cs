using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyStates
{
    GUARD,
    PARTOL,
    CHASE,
    DEATH
}

public abstract class EnemyHandler : NetworkBehaviour
{
    [Header("Enemy Setting")]
    public float sightRadius;
    public bool isGuard;
    private float speed;
    public float partolTotalWaitTime;
    private Timer partrolWaitTime;



    [Header("Partrol State")]
    public float partolRange;
    private Vector3 wayPoint;
    private Vector3 guardPos;

    //Enemy bool 狀態
    private bool isDeath;

    private EnemyStates enemyStates;
    public GameObject attackTarget;

    //Other components
    private NavMeshAgent agent;


    #region  MonoBehavior
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        guardPos = transform.position;
        speed = agent.speed;
    }

    public override void Spawned()
    {
        if (isGuard)
        {
            enemyStates = EnemyStates.GUARD;
        }
        else
        {
            enemyStates = EnemyStates.PARTOL;
            GetNewWayPoint();
        }
    }

    #endregion

    public override void FixedUpdateNetwork()
    {
        EnemyState();
    }

    public abstract void EnemyGuard();
    public virtual void EnemyPartol()
    {
        // isChase = false;
        agent.speed = speed * 0.5f;

        //判斷是否到了隨機巡邏點
        if (Vector3.Distance(wayPoint, transform.position) <= agent.stoppingDistance)
        {
            // isWalk = false;
            if (!partrolWaitTime.IsRunning)
                partrolWaitTime.Start();

            if (partrolWaitTime.ElapsedInSeconds >= partolTotalWaitTime)
                GetNewWayPoint();
        }
        else
        {
            // isWalk = true;
            agent.destination = wayPoint;
        }
    }

    public abstract void EnemyChase();
    public abstract void EnemyDeath();


    //敵人狀態模式
    private void EnemyState()
    {
        //當敵人死亡時
        if (isDeath)
        {
            enemyStates = EnemyStates.DEATH;
        }

        //當找到玩家時
        // else if (FoundPlayer())
        // {
        //     enemyStates = EnemyStates.CHASE;
        // }

        //敵人狀態
        switch (enemyStates)
        {
            case EnemyStates.GUARD:
                EnemyGuard();
                break;
            case EnemyStates.PARTOL:
                EnemyPartol();
                break;
            case EnemyStates.CHASE:
                EnemyChase();
                break;
            case EnemyStates.DEATH:
                EnemyDeath();
                break;
        }
    }

    //找出新的移動位值
    private void GetNewWayPoint()
    {
        float randomX = Random.Range(-partolRange, partolRange);
        float randomZ = Random.Range(-partolRange, partolRange);

        Vector3 randomPoint = new Vector3(guardPos.x + randomX, transform.position.y, guardPos.z + randomZ);

        NavMeshHit hit;
        //NavMesh.SamplePosition 在指定範圍內找到導航網格上最近的點
        wayPoint = NavMesh.SamplePosition(randomPoint, out hit, partolRange, 1) ? hit.position : transform.position;
        partrolWaitTime.Restart();
        partrolWaitTime.Stop();
    }

    //是否在範圍內找到玩家
    private bool FoundPlayer()
    {
        //OverlapSphere 返回一個數組,其中包含球體內部的所有碰撞體
        Collider[] collider = Physics.OverlapSphere(transform.position, sightRadius);

        foreach (Collider target in collider)
        {
            if (target.CompareTag("Player"))
            {
                attackTarget = target.gameObject;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 在Unity畫出實線範圍
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, sightRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, partolRange);
    }
}
