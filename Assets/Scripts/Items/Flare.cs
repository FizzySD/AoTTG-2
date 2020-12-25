﻿using Assets.Scripts.Serialization;
using Newtonsoft.Json;
using UnityEngine;

namespace Assets.Scripts.Items
{
    public class Flare : Item
    {
        private const float FlareCooldown = 30f;
        private float Cooldown { get; set; } = -FlareCooldown;
        private readonly Color _color;

        public Flare(Color color)
        {
            _color = color;
        }

        public override void Use(Hero user)
        {
            if (Time.time - (Cooldown + FlareCooldown) <= 0f) return;
            Cooldown = Time.time;

            var flare = PhotonNetwork.Instantiate("FX/flare", user.transform.position,
                Quaternion.LookRotation(Camera.main.transform.forward), 0).GetComponent<FlareMovement>();
            flare.HideHint();
            var json = JsonConvert.SerializeObject(_color, Formatting.Indented, new ColorJsonConverter());
            flare.photonView.RPC(nameof(FlareMovement.SetColorRpc), PhotonTargets.All, json);
        }
    }
}
