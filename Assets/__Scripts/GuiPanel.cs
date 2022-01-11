using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GuiPanel : MonoBehaviour
{
    [Header("Set in Inspector")]
    public Dray dray;
    public Sprite healthEmpty;
    public Sprite healthHalf;
    public Sprite healthFull;

    Text keyCountText;
    List<Image> healthImage;

    void Start()
    {
        //Счетчик ключей 
        Transform trans = transform.Find("Key Count");
        keyCountText = trans.GetComponent<Text>();

        //индикатор счетчика здоровья 
        Transform healthPanel = transform.Find("Health Panel");
        healthImage = new List<Image>();
        if(healthPanel != null)
        {
            for(int i=0; i<20; i++)
            {
                trans = healthPanel.Find("H_" + i);
                if (trans == null) break;
                healthImage.Add(trans.GetComponent<Image>());
            }
        }
    }
    void Update()
    {
        //показать колличество ключей 
        keyCountText.text = dray.numKeys.ToString();

        //показать уровень здоровья
        int health = dray.health;
        for(int i = 0; i<healthImage.Count; i++)
        {
            if(health > 1)
            {
                healthImage[i].sprite = healthFull;
            }
            else if(health == 1)
            {
                healthImage[i].sprite = healthHalf;
            }
            else
            {
                healthImage[i].sprite = healthEmpty;
            }
            health -= 2;
        }
    }
}
