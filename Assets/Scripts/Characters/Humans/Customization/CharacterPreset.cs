﻿using Assets.Scripts.Characters.Humans.Customization.Components;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.Characters.Humans.Customization
{
    [CreateAssetMenu(fileName = "New Character Preset", menuName = "Character/Character Preset")]
    public class CharacterPreset : ScriptableObject
    {
        [SerializeField] public string Name;
        [SerializeField] public string Description;
        [SerializeField] public List<CharacterOutfit> CharacterOutfit;
        [SerializeField] public List<CharacterBuild> CharacterBuild;

        public CharacterOutfit CurrentOutfit { get; set; }
        public CharacterBuild CurrentBuild { get; set; }

        private CharacterPrefabs Prefabs;
        private HumanBody Body;
        private Transform HumanTransform { get; set; }

        public async Task Apply(Human human, CharacterPrefabs prefabs)
        {
            Prefabs = prefabs;
            CurrentOutfit = CharacterOutfit[0];
            CurrentBuild = CharacterBuild[0];
            Body = human.Body;
            HumanTransform = human.transform;

            var skin = Prefabs.GetSkinPrefab(CurrentOutfit.Skin.Skin);

            CreateHead(CurrentOutfit.Head, skin);
            CreateHair(CurrentOutfit.Hair);
            CreateOutfit(CurrentOutfit.Outfit);
            CreateCape(CurrentOutfit.Cape);
            CreateEmblems();
            CreateEquipment(CurrentBuild.EquipmentComponent);

            await CreateEyes(CurrentOutfit.Eyes);
        }

        private GameObject CreateComponent(GameObject prefab, Texture2D texture, Color color = default, Transform parent = null)
        {
            var prefabObject = Instantiate(prefab);
            if (prefabObject.TryGetComponent(out SkinnedMeshRenderer meshRenderer))
            {
                meshRenderer.material.mainTexture = texture;
                if (color != default)
                    meshRenderer.material.color = color;

                meshRenderer.rootBone = Body.ControllerBody;
                meshRenderer.bones = Body.Bones;
                prefabObject.transform.parent = parent ?? Body.ControllerBody;
                //prefabObject.transform.position = HumanTransform.position;
                //prefabObject.transform.rotation = HumanTransform.rotation;
            }

            if (prefabObject.TryGetComponent(out Renderer renderer))
            {
                if (color != default)
                    renderer.material.color = color;

                renderer.material.mainTexture = texture;
                prefabObject.transform.parent = parent ?? Body.ControllerBody;
                //prefabObject.transform.position = HumanTransform.position;
                //prefabObject.transform.rotation = HumanTransform.rotation;
            }

            //prefabObject.transform.position = parent?.transform.position ?? Body.ControllerBody.position;

            return prefabObject;
        }

        private GameObject CreateComponent(GameObject prefab, Texture2D texture, Transform parent)
        {
            return CreateComponent(prefab, texture, default, parent);
        }

        private async Task<GameObject> CreateComponentAsync(GameObject prefab, string textureUrl, Color color = default,
            Transform parent = null)
        {
            var texture = await GetRemoteTexture(textureUrl);
            return CreateComponent(prefab, texture, color, parent);
        }

        private void CreateHead(HeadComponent head, SkinPrefab skin)
        {
            var result = CreateComponent(Prefabs.GetHeadPrefab(head.Model).Prefab, skin.File, Body.head);
            result.transform.position = HumanTransform.position;
            result.transform.rotation = Quaternion.Euler(270f, HumanTransform.rotation.eulerAngles.y, 0f);
        }

        private async Task CreateHair(HairComponent hair)
        {
            var prefab = Prefabs.GetHairPrefab(hair.Model);

            if (!string.IsNullOrWhiteSpace(hair.CustomUrl))
            {
                var result = await CreateComponentAsync(prefab.Prefab, hair.CustomUrl, hair.Color, Body.head);
                result.transform.position = Body.head.position;
                result.transform.rotation = Body.head.rotation;
            }
            else
            {
                var texture = prefab.GetTexture(hair.Texture);
                var result = CreateComponent(prefab.Prefab, texture.File, hair.Color, Body.head);
                result.transform.position = HumanTransform.position;
                result.transform.rotation = Quaternion.Euler(270f, HumanTransform.rotation.eulerAngles.y, 0f);
            }
        }

        private async Task CreateEyes(EyesComponent eyes)
        {
            var prefab = Prefabs.Eyes;

            if (!string.IsNullOrWhiteSpace(eyes.CustomUrl))
            {
                var result = await CreateComponentAsync(prefab.Prefab, eyes.CustomUrl, eyes.Color, Body.ControllerBody);
            }
            else
            {
                var texture = prefab.GetTexture(eyes.Texture);
                var result = CreateComponent(prefab.Prefab, texture.File, eyes.Color, Body.head);
                result.transform.position = HumanTransform.position;
                result.transform.rotation = Quaternion.Euler(270f, HumanTransform.rotation.eulerAngles.y, 0f);
            }
        }

        private void CreateOutfit(OutfitComponent outfit)
        {
            var prefab = Prefabs.GetOutfitPrefab(outfit.Model);
            var texture = prefab.GetTexture(outfit.Texture);
            CreateComponent(prefab.Prefab, texture.File, outfit.Color);

            CreateLegs(outfit);
            CreateArms(outfit);
        }

        private void CreateLegs(OutfitComponent outfit)
        {
            var prefab = Prefabs.GetOutfitPrefab(outfit.Model);
            var texture = prefab.GetTexture(outfit.Texture);
            CreateComponent(Prefabs.Legs, texture.File, outfit.Color);
        }

        private void CreateArms(OutfitComponent outfit)
        {
            var prefab = Prefabs.GetOutfitPrefab(outfit.Model);
            var texture = prefab.GetTexture(outfit.Texture);

            GameObject armLeft, armRight;
            if (outfit.Model == OutfitModel.CasualFemaleA || outfit.Model == OutfitModel.CasualMaleA)
            {
                armLeft = Prefabs.Arms.LeftCasual;
                armRight = Prefabs.Arms.RightCasual;
            }
            else if (outfit.Model == OutfitModel.CasualFemaleB || outfit.Model == OutfitModel.CasualMaleB)
            {
                armLeft = Prefabs.Arms.LeftAhss;
                armRight = Prefabs.Arms.RightAhss;
            }
            else
            {
                armLeft = Prefabs.Arms.LeftUniform;
                armRight = Prefabs.Arms.RightUniform;
            }

            CreateComponent(armLeft, texture.File, outfit.Color);
            CreateComponent(armRight, texture.File, outfit.Color);
        }

        private void CreateEquipment(EquipmentComponent equipment)
        {
            var prefab = Prefabs.GetEquipmentPrefab(CurrentBuild.Equipment);
            var skin = prefab.GetTexture(equipment.HandGrid.Texture);
            var ammo = prefab.GetTexture(equipment.Texture).File;

            var handLeft = Instantiate(prefab.HandLeft);
            var handRight = Instantiate(prefab.HandRight);
            var ammoLeft = Instantiate(prefab.AmmoLeft);
            var ammoRight = Instantiate(prefab.AmmoRight);

            handLeft.GetComponent<Renderer>().material.mainTexture = skin.File;
            handRight.GetComponent<Renderer>().material.mainTexture = skin.File;
            ammoLeft.GetComponent<Renderer>().material.mainTexture = ammo;
            ammoRight.GetComponent<Renderer>().material.mainTexture = ammo;

            ammoLeft.transform.parent = Body.ControllerBody;
            ammoLeft.transform.position = HumanTransform.position;
            ammoLeft.transform.rotation = Quaternion.Euler(270f, HumanTransform.rotation.eulerAngles.y, 0f);

            ammoRight.transform.parent = Body.ControllerBody;
            ammoRight.transform.position = HumanTransform.position;
            ammoRight.transform.rotation = Quaternion.Euler(270f, HumanTransform.rotation.eulerAngles.y, 0f);

            handLeft.transform.parent = Body.hand_L;
            handLeft.transform.position = HumanTransform.position;
            handLeft.transform.rotation = Quaternion.Euler(270f, HumanTransform.rotation.eulerAngles.y, 0f);

            handRight.transform.parent = Body.hand_R;
            handRight.transform.position = HumanTransform.position;
            handRight.transform.rotation = Quaternion.Euler(270f, HumanTransform.rotation.eulerAngles.y, 0f);

            CreateOdmg(equipment);
        }

        private void CreateOdmg(EquipmentComponent equipment)
        {
            var prefab = Prefabs.GetEquipmentPrefab(CurrentBuild.Equipment);
            var texture = prefab.GetTexture(equipment.Texture);
            var result = CreateComponent(prefab.Equipment, texture.File, equipment.Color);
            result.transform.parent = Body.chest;
            result.transform.position = HumanTransform.position;
            result.transform.rotation = Quaternion.Euler(270f, HumanTransform.rotation.eulerAngles.y, 0f);

            foreach (var extra in prefab.Extras)
            {
                result = CreateComponent(extra, texture.File, equipment.Color, HumanTransform);
                result.transform.parent = Body.ControllerBody;
                result.transform.position = HumanTransform.position;
            }
        }

        private void CreateCape(CapeComponent cape)
        {
            if (cape.Texture == CapeTexture.None) return;
            var prefab = Prefabs.Cape;
            var texture = prefab.GetTexture(cape.Texture);
            var capeObject = CreateComponent(prefab.Prefab, texture.File, cape.Color);
            //capeObject.AddComponent<ParentFollow>().SetParent(Body.transform.parent.transform);
            capeObject.transform.localScale = Vector3.one;
        }

        private void CreateEmblems()
        {
            var texture = Prefabs.Cape.GetTexture(CurrentOutfit.Cape.Texture).File;
            var prefab = Prefabs.Emblem;
            var gender = CurrentOutfit.Gender;

            var emblems = new List<GameObject>
                {prefab.ArmLeft, prefab.ArmRight, prefab.GetBackPrefab(gender), prefab.GetChestPrefab(gender)};

            emblems.ForEach(x => CreateComponent(x, texture));
        }

        private static async Task<Texture2D> GetRemoteTexture(string url)
        {
            using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
            {
                //begin requenst:
                var asyncOp = www.SendWebRequest();

                //await until it's done: 
                while (asyncOp.isDone == false)
                {
                    await Task.Delay(1000 / 30);//30 hertz
                }

                //read results:
                if (www.isNetworkError || www.isHttpError)
                {
                    //log error:
#if DEBUG
                    Debug.Log($"{ www.error }, URL:{ www.url }");
#endif

                    //nothing to return on error:
                    return null;
                }
                else
                {
                    //return valid results:
                    return DownloadHandlerTexture.GetContent(www);
                }
            }
        }
    }
}
