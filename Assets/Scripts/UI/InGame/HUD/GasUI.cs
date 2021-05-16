using Assets.Scripts.Characters.Humans;
using Assets.Scripts.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI.InGame.HUD
{
    public class GasUI : UiElement
    {
        [SerializeField] private Image leftImage;
        [SerializeField] private Image rightImage;

        private void Awake() => Hero.OnUseGasClient += UpdateGas;

        private void UpdateGas(Hero hero, float percent)
        {
            leftImage.fillAmount = percent;
            rightImage.fillAmount = percent;

            var fillColor = Color.white;
            if (percent <= 0.25f)
                fillColor = Color.red;
            else if (percent <= 0.5f)
                fillColor = Color.yellow;

            leftImage.color = fillColor;
            rightImage.color = fillColor;
        }
    }
}