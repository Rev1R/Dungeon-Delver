using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dray : MonoBehaviour, IFacingMover, IKeyMaster
{
    public enum eMode { idle, move, attack, transition, knockback}

    [Header("Set in Inspector")]
    public float speed = 5;
    public float attackDuration = 0.25f;    //продолжительность атаки в секундах
    public float attackDelay = 0.5f;        //задержка между атаками
    public float transitionDelay = 0.5f;  //задержка перехода между комнатами //а

    public int maxHealth = 10;
    public float knockbackSpeed = 10;
    public float knockbackDuration = 0.25f;
    public float invincibleDuration = 0.5f;

    [Header("Set Dynamiccaly")]
    public int dirHeld = -1;   //направление соответствующее удерживаемой клавише
    public int facing = 1;   //направление движения дрея
    public eMode mode = eMode.idle;
    public int numKeys = 0;
    public bool invincible = false;
    public bool hasGrappler = false;
    public Vector3 lastSafeLoc;
    public int lastSafeFacing;

    [SerializeField]
    private int _health;  

    public int health
    {
        get { return _health; }
        set { _health = value; }
    }

    private float timeAtkDone = 0;
    private float timeAtkNext = 0;

    private float transitionDone = 0;
    private Vector2 transitionPos;
    private float knockbackDone = 0;
    private float invincibleDone = 0;
    private Vector3 knockbackVel;

    private SpriteRenderer sRend;
    private Rigidbody rigid;
    private Animator anim;
    private InRoom inRm;
    private Vector3[] directions = new Vector3[] {Vector3.right, Vector3.up, Vector3.left, Vector3.down };
    private KeyCode[] keys = new KeyCode[] { KeyCode.RightArrow, KeyCode.UpArrow, KeyCode.LeftArrow, KeyCode.DownArrow };

    void Awake()
    {
        sRend = GetComponent<SpriteRenderer>();
        rigid = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        inRm = GetComponent<InRoom>();
        health = maxHealth;
        lastSafeLoc = transform.position;  //начальная позиция безопасна
        lastSafeFacing = facing;
    }
    void Update()
    {
        //проверить состояние неуязвимости и необходимость выполнить отбрасывание
        if (invincible && Time.time > invincibleDone) invincible = false;
        sRend.color = invincible ? Color.red : Color.white;
        if(mode == eMode.knockback)
        {
            rigid.velocity = knockbackVel;
            if (Time.time < knockbackDone) return;
        }
        if(mode == eMode.transition)        //b
        {
            rigid.velocity = Vector3.zero;
            anim.speed = 0;
            roomPos = transitionPos;   //оставить Дрея на месте
            if (Time.time < transitionDone) return;
            //Следующая строка выполнится, только если Time.time >= transitionDone
            mode = eMode.idle;
        }
        //обработка ввода с клавиатуры и управление режимами eMode
        dirHeld = -1;
       for(int i=0; i<4; i++)
        {
            if (Input.GetKey(keys[i])) dirHeld = i;
        }

       //нажата клавиша атаки
       if(Input.GetKeyDown(KeyCode.Z)&& Time.time >= timeAtkNext)       //a
        {
            mode = eMode.attack;
            timeAtkDone = Time.time + attackDuration;
            timeAtkNext = Time.time + attackDelay;
        }
       //завершить атаку если время истекло
       if(Time.time >= timeAtkDone)                                       //b
        {
            mode = eMode.idle;
        }
       //выбрать правильный режим если дрей не атакует
       if(mode != eMode.attack)                                           //c
        {
            if(dirHeld == -1)
            {
                mode = eMode.idle;
            }
            else
            {
                facing = dirHeld;                                          //d
                mode = eMode.move;
            }
        }
        //действия в текущем режиме
        Vector3 vel = Vector3.zero;
        switch (mode)                                                      //e
        {
            case eMode.attack:
                anim.CrossFade("Dray_Attack_" + facing, 0);
                anim.speed = 0;
                break;
            case eMode.idle:
                anim.CrossFade("Dray_Walk_" + facing, 0);
                anim.speed = 0;
                break;
            case eMode.move:
                vel = directions[dirHeld];
                anim.CrossFade("Dray_Walk_" + facing, 0);
                anim.speed = 1;
                break;
        }
        rigid.velocity = vel * speed;
    }
    void LateUpdate()
    {
        //получить координаты узла сетки, с размером ячейки
        //в половину единицы, ближайшего к данному персонажу
        Vector2 rPos = GetRoomPosOnGrid(0.5f);   //размер ячейки в пол-единицы  //c

        //персонаж находится на плитке с дверью?
        int doorNum;
        for(doorNum=0; doorNum < 4; doorNum++)
        {
            if (rPos == InRoom.DOORS[doorNum])
            {
                break;                              //d
            }
        }
        if (doorNum > 3 || doorNum != facing) return;     //e

        //перейти в следующую комнату 
        Vector2 rm = roomNum;
        switch (doorNum)                      //f
        {
            case 0:
                rm.x += 1;
                break;
            case 1:
                rm.y += 1;
                break;
            case 2:
                rm.x -= 1;
                break;
            case 3:
                rm.y -= 1;
                break;
        }
        //проверить, можно ли выполнить переход в комнату rm
        if(rm.x >= 0 && rm.x <= InRoom.MAX_RM_X)                   //g
        {
            if(rm.y >= 0 && rm.y <= InRoom.MAX_RM_Y)
            {
                roomNum = rm;
                transitionPos = InRoom.DOORS[(doorNum + 2) % 4];   //h
                roomPos = transitionPos;
                lastSafeLoc = transform.position;
                lastSafeFacing = facing;
                mode = eMode.transition;
                transitionDone = Time.time + transitionDelay;
            }
        }
    }
    void OnCollisionEnter(Collision coll)
    {
        if (invincible) return;  //выйти если Дрей пока неуязвим
        DamageEffect dEf = coll.gameObject.GetComponent<DamageEffect>();
        if (dEf == null) return;   //если компонент DamageEffect отсутствует - выйти

        health -= dEf.damage; //вычесть величину ущерба из уровня здоровья
        invincible = true;  //сделать дрея неуязвимым
        invincibleDone = Time.time + invincibleDuration;

        if (dEf.knockback)  //выполнить отбрасывание
            //определить направление отбрасывания
        {
            Vector3 delta = transform.position - coll.transform.position;
            if(Mathf.Abs(delta.x)>= Mathf.Abs(delta.y))
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
            //применить скорость отскока к компоненту RigeidBody
            knockbackVel = delta * knockbackSpeed;
            rigid.velocity = knockbackVel;

            //установить режим knockback и время прекращения отбрасывания
            mode = eMode.knockback;
            knockbackDone = Time.time + knockbackDuration;
        }
    }
    void OnTriggerEnter(Collider colld)
    {
        PickUp pup = colld.GetComponent<PickUp>();
        if (pup == null) return;

        switch (pup.itemType)
        {
            case PickUp.eType.health:
                health = Mathf.Min(health + 2, maxHealth);
                break;
            case PickUp.eType.key:
                keyCount++;
                break;
            case PickUp.eType.grappler:
                hasGrappler = true;
                break;
        }
        Destroy(colld.gameObject);
    }
    public void ResetInRoom(int healthLoss = 0)
    {
        transform.position = lastSafeLoc;
        facing = lastSafeFacing;
        health -= healthLoss;

        invincible = true;  //сделать Дрея неуязвимым
        invincibleDone = Time.time + invincibleDuration;
    }
    //реализация интерфейса IFacingMover
    public int GetFacing()
    {
        return facing;
    }
    public bool moving
    {
        get
        {
            return (mode == eMode.move);
        }
    }
    public float GetSpeed()
    {
        return speed;
    }
    public float gridMult
    {
        get { return inRm.gridMult;  }
    }
    public Vector2 roomPos
    {
        get { return inRm.roomPos; }
        set { inRm.roomPos = value; }
    }
    public Vector2 roomNum
    {
        get { return inRm.roomNum; }
        set { inRm.roomNum = value; }
    }
    public Vector2 GetRoomPosOnGrid ( float mult = -1)
    {
        return inRm.GetRoomPosOnGrid(mult);
    }
    //реализация интерфейса IKeyMaster
    public int keyCount
    {
        get { return numKeys; }
        set { numKeys = value; }
    }
}
