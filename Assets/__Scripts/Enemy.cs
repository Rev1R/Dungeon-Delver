﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    protected static Vector3[] directions = new Vector3[] { Vector3.right, Vector3.up, Vector3.left, Vector3.down };

    [Header("Set in Inspector: Enemy")]
    public float maxHealth = 1;
    public float knockbackSpeed = 10;
    public float knockbackDuration = 0.25f;
    public float invincibleDuration = 0.5f;
    public GameObject[] randomItemDrops;
    public GameObject guaranteedItemDrop = null;



    [Header("Set Dynamically: Enemy")]
    public float health;
    public bool invincible = false;
    public bool knockback = false;

    private float invincibleDone = 0;
    private float knockbackDone = 0;
    private Vector3 knockbackVel;

    protected Animator anim;
    protected Rigidbody rigid;
    protected SpriteRenderer sRend;

    protected virtual void Awake()
    {
        health = maxHealth;
        anim = GetComponent<Animator>();
        rigid = GetComponent<Rigidbody>();
        sRend = GetComponent<SpriteRenderer>();
    }
    protected virtual void Update()
    {
        //проверить состояние неуязвимости и необходимость выполнить отскок
        if (invincible && Time.time > invincibleDone) invincible = false;
        sRend.color = invincible ? Color.red : Color.white;
        if (knockback)
        {
            rigid.velocity = knockbackVel;
            if (Time.time < knockbackDone) return;
        }
        anim.speed = 1;
        knockback = false;
    }
    void OnTriggerEnter(Collider colld)
    {
        if (invincible) return;  //выйти, если дрей пока неуязвим
        DamageEffect dEf = colld.gameObject.GetComponent<DamageEffect>();
        if (dEf == null) return;  //если компонент DamageEffect отсутствует - выйти

        health -= dEf.damage;  //вычесть велечину ущерба из уровня здоровья
        if (health <= 0) Die();
        invincible = true;  //сделать Дрея неуязвимым
        invincibleDone = Time.time + invincibleDuration;

        if (dEf.knockback)   //выполнить отбрасывание
        {
            //определить направление отскока
            Vector3 delta = transform.position - colld.transform.root.position;
            if(Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            {
                //отбрасывание по горизонтали
                delta.x = (delta.x > 0) ? 1 : -1;
                delta.y = 0;
            }
            else
            {
                //отбрасывание по вертикали 
                delta.x = 0;
                delta.y = (delta.y > 0) ? 1 : -1;
            }
            //применить скорость отбрасывания к компоненту RigidBody 
            knockbackVel = delta * knockbackSpeed;
            rigid.velocity = knockbackVel;

            //установить режим knockback и время прекращения отбрасывания 
            knockback = true;
            knockbackDone = Time.time + knockbackDuration;
            anim.speed = 0;
        }
    }
    void Die()
    {
        GameObject go;
        if(guaranteedItemDrop != null)
        {
            go = Instantiate<GameObject>(guaranteedItemDrop);
            go.transform.position = transform.position;
        }
        else if(randomItemDrops.Length > 0)
        {
            int n = Random.Range(0, randomItemDrops.Length);
            GameObject prefab = randomItemDrops[n];
            if(prefab != null)
            {
                go = Instantiate<GameObject>(prefab);
                go.transform.position = transform.position;
            }
        }
        Destroy(gameObject);
    }
}
