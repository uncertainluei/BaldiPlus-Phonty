﻿using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Components;

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using TMPro;
using UnityEngine;
using UnityEngine.Audio;

namespace PhontyPlus
{
    public class Phonty : NPC, IClickable<int>
    {
        public static Sprite idle;
        public static List<SoundObject> records = new List<SoundObject>();
        public static AssetManager audios = new AssetManager();

        public static List<Sprite> emergeFrames = new List<Sprite>();
        public static List<Sprite> chaseFrames = new List<Sprite>();

        public CustomSpriteAnimator animator;
        public AudioManager audMan;
        public TextMeshPro counter;
        public GameObject totalDisplay;

        public MapIcon mapIconPre;
        public NoLateIcon mapIcon;

        public bool angry = false;
        private bool deafPlayer = false;
        public static void LoadAssets() {
            var PIXELS_PER_UNIT = 26f;
            idle = AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(Mod.Instance, "Textures", "Phonty_Idle.png"), PIXELS_PER_UNIT);
            audios.Add("angryIntro", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(Mod.Instance, "Audio", "PhontyIntro.ogg"), "Phonty_Vfx_Intro", SoundType.Voice, Color.yellow));
            audios.Add("angry", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(Mod.Instance, "Audio", "PhontyAngry.ogg"), "Phonty_Vfx_Angry", SoundType.Voice, Color.yellow));
            audios.Add("shockwave", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(Mod.Instance, "Audio", "PhontyShot.ogg"), "Phonty_Sfx_Shot", SoundType.Effect, Color.yellow));

            emergeFrames.AddRange(AssetLoader.SpritesFromSpritesheet(4, 4, PIXELS_PER_UNIT, Vector2.one / 2f, AssetLoader.TextureFromMod(Mod.Instance, "Textures", "Phonty_Emerge0.png")));
            emergeFrames.AddRange(AssetLoader.SpritesFromSpritesheet(4, 4, PIXELS_PER_UNIT, Vector2.one / 2f, AssetLoader.TextureFromMod(Mod.Instance, "Textures", "Phonty_Emerge1.png")));

            Sprite[] emergeSheet2 = AssetLoader.SpritesFromSpritesheet(4, 2, PIXELS_PER_UNIT, Vector2.one / 2f, AssetLoader.TextureFromMod(Mod.Instance, "Textures", "Phonty_Emerge2.png"));
            for (int i = 7; i > 5; i--)
                DestroyImmediate(emergeSheet2[i]); // Remove blank textures
            for (int i = 0; i < 6; i++)
                emergeFrames.Add(emergeSheet2[i]);

            chaseFrames.AddRange(AssetLoader.SpritesFromSpritesheet(4, 4, PIXELS_PER_UNIT, Vector2.one / 2f, AssetLoader.TextureFromMod(Mod.Instance, "Textures", "Phonty_Chase0.png")));
            chaseFrames.AddRange(AssetLoader.SpritesFromSpritesheet(4, 1, PIXELS_PER_UNIT, Vector2.one / 2f, AssetLoader.TextureFromMod(Mod.Instance, "Textures", "Phonty_Chase1.png")));

            var recordsFolder = Directory.GetFiles(Path.Combine(AssetLoader.GetModPath(Mod  .Instance), "Audio", "Records"));
            foreach (var path in recordsFolder)
                records.Add(ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(path), "Phonty_Vfx_Record", SoundType.Voice, Color.yellow));
        }

        public bool ClickableHidden() => angry;

        public bool ClickableRequiresNormalHeight() => false;

        public void ClickableSighted(int player) {}

        public void ClickableUnsighted(int player) {}

        public void Clicked(int player)
        {
            if (!angry)
                ResetTimer();
        }
        private IEnumerator DeafenPlayer()
        {
            yield return new WaitForSeconds(0.5f);

            AudioListener.volume = 0.01f;
            Mod.assetManager.Get<AudioMixer>("Mixer").SetFloat("EchoWetMix", 1f);
            deafPlayer = true;
            yield break;
        }

        public void EndGame(Transform player)
        {
            var core = CoreGameManager.Instance;
            if (PhontyMenu.nonLethalConfig.Value == true || CoreGameManager.Instance.currentMode == Mode.Free)
            {
                core.audMan.PlaySingle(audios.Get<SoundObject>("shockwave"));
                behaviorStateMachine.ChangeState(new Phonty_Dead(this));
                StartCoroutine(DeafenPlayer());
                return;
            }
            Time.timeScale = 0f;
            MusicManager.Instance.StopMidi();
            core.disablePause = true;
            core.GetCamera(0).UpdateTargets(transform, 0);
            core.GetCamera(0).offestPos = (player.position - transform.position).normalized * 2f + Vector3.up;
            core.GetCamera(0).SetControllable(false);
            core.GetCamera(0).matchTargetRotation = false;
            core.audMan.volumeModifier = 0.6f;
            core.audMan.PlaySingle(audios.Get<SoundObject>("shockwave"));
            core.StartCoroutine(core.EndSequence());
            InputManager.Instance.Rumble(1f, 2f);
            HighlightManager.Instance.Highlight("steam_x",
                LocalizationManager.Instance.GetLocalizedText("Steam_Highlight_Lose"),
                string.Format(LocalizationManager.Instance.GetLocalizedText("Steam_Highlight_Lose_Desc"),
                              LocalizationManager.Instance.GetLocalizedText(BaseGameManager.Instance.managerNameKey),
                              LocalizationManager.Instance.GetLocalizedText(CoreGameManager.Instance.sceneObject.nameKey)), 2U, 0f, 0f, TimelineEventClipPriority.Standard);
        }
        public override void Initialize()
        {
            base.Initialize();
            animator.animations.Add("Idle", new CustomAnimation<Sprite>(new Sprite[] { idle }, 1f));
            animator.animations.Add("Chase", new CustomAnimation<Sprite>(chaseFrames.ToArray(), 0.5f));
            animator.animations.Add("ChaseStatic", new CustomAnimation<Sprite>(new Sprite[] { emergeFrames.Last() }, 1f));
            animator.animations.Add("Emerge", new CustomAnimation<Sprite>(emergeFrames.ToArray(), 1f));
            animator.SetDefaultAnimation("Idle", 1f);

            // Counter on top of Phonty
            var totalBase = GameObject.Instantiate( Mod.assetManager.Get<GameObject>("TotalBase"), transform);
            totalBase.transform.localPosition = new Vector3(0,3,0);
            totalBase.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            totalBase.SetActive(true);
            totalDisplay = totalBase.gameObject.transform.GetChild(0).gameObject;
            totalDisplay.SetActive(true);
            counter = totalDisplay.GetComponent<TextMeshPro>();

            mapIconPre = Mod.assetManager.Get<NoLateIcon>("MapIcon");
            mapIcon = (NoLateIcon)ec.map.AddIcon(mapIconPre, gameObject.transform, Color.white);
            mapIcon.spriteRenderer.sprite = idle;
            DestroyImmediate(mapIcon.GetComponent<Animator>());
            mapIcon.gameObject.SetActive(true);

            behaviorStateMachine.ChangeState(new Phonty_PlayingMusic(this));
            navigator.SetSpeed(0f);
            navigator.maxSpeed = 0f;
            navigator.Entity.SetHeight(7f);
            navigator.Entity.defaultLayer = LayerMask.NameToLayer("ClickableEntities");
            gameObject.layer = Navigator.Entity.defaultLayer;

            var position = IntVector2.GetGridPosition(gameObject.transform.position);
            var cell = ec.CellFromPosition(position);
            var startingRoom = cell.room;
            for (int i = 0; i < startingRoom.TileCount; i++)
            {
                ec.map.Find(startingRoom.TileAtIndex(i).position.x, startingRoom.TileAtIndex(i).position.z, startingRoom.TileAtIndex(i).ConstBin, startingRoom);
            }
        }

        public override void VirtualUpdate()
        {
            if (deafPlayer && AudioListener.volume > 0.01f)
                AudioListener.volume = 0.01f;
        }

        public void ResetTimer()
        {
            behaviorStateMachine.ChangeState(new Phonty_PlayingMusic(this, true));
        }

        private int currentDisplayTime = -1;

        public void UpdateCounter(int count)
        {
            if (count != currentDisplayTime)
            {
                currentDisplayTime = Mathf.Max(count, 0);
                mapIcon.timeText.text = Mathf.Floor(currentDisplayTime / 60).ToString("0") + ":" + (currentDisplayTime % 60).ToString("00");
                mapIcon.UpdatePosition(ec.map);
                counter.SetText(string.Join("", count.ToString().Select(ch => "<sprite=" + ch + ">")));
            }
        }
    }

    public class Phonty_StateBase : NpcState
    {
        public Phonty_StateBase(Phonty phonty) : base(phonty)
        {
            this.phonty = phonty;
        }
        public Phonty phonty;

    }
    public class Phonty_PlayingMusic : Phonty_StateBase
    {
        private bool interacted;
        public Phonty_PlayingMusic(Phonty phonty, bool playerInteraction = false) : base(phonty)
        {
            interacted = playerInteraction;
        }
        public override void Enter()
        {
            base.Enter();
            base.ChangeNavigationState(new NavigationState_DoNothing(phonty, 63));
            phonty.audMan.FlushQueue(true);
            if (interacted)
                phonty.audMan.PlaySingle(Mod.assetManager.Get<SoundObject>("windup"));
            phonty.audMan.QueueRandomAudio(Phonty.records.ToArray());
            phonty.audMan.SetLoop(true);
            phonty.animator.Play("Idle", 1f);
#if DEBUG
            timeLeft = 10;
#endif
        }
        public override void Update()
        {
            base.Update();
            if (timeLeft <= 0)
            {
                phonty.behaviorStateMachine.ChangeState(new Phonty_Chase(phonty));
            }
            else {
                timeLeft -= Time.deltaTime * phonty.ec.NpcTimeScale;
            }
            phonty.UpdateCounter((int)timeLeft);
        }
        protected float timeLeft = PhontyMenu.timeLeftUntilMad.Value;
    }
    public class Phonty_Chase : Phonty_StateBase
    {
        protected NavigationState_TargetPlayer targetState;
        protected PlayerManager player;
        public Phonty_Chase(Phonty phonty) : base(phonty)
        {
            player = phonty.ec.Players[0];
            targetState = new NavigationState_TargetPlayer(phonty, 64, player.transform.position);
        }
        public override void Enter()
        {
            base.Enter();
            targetState = new NavigationState_TargetPlayer(phonty, 64, player.transform.position);
            base.ChangeNavigationState(targetState);
            phonty.angry = true;
            phonty.totalDisplay.SetActive(false);
            phonty.mapIcon.gameObject.SetActive(false);

            phonty.audMan.FlushQueue(true);
            phonty.audMan.QueueAudio(Phonty.audios.Get<SoundObject>("angryIntro"), true);

            phonty.StartCoroutine(Emerge());
            phonty.animator.Play("Emerge", 1f);
            phonty.animator.SetDefaultAnimation("ChaseStatic", 1f);
        }
        public override void DestinationEmpty()
        {
            base.DestinationEmpty();
            base.ChangeNavigationState(new NavigationState_WanderRandom(phonty, 32));
        }
        public override void PlayerInSight(PlayerManager player)
        {
            base.PlayerInSight(player);
            if (this.player == player)
            {
                base.ChangeNavigationState(targetState);
                targetState.UpdatePosition(player.transform.position);
            }
        }
        public override void OnStateTriggerEnter(Collider other)
        {
            base.OnStateTriggerEnter(other);
            if (other.CompareTag("Player") && other.GetComponent<PlayerManager>() == player)
            {
                phonty.EndGame(other.transform);
            }
        }

        private IEnumerator Emerge()
        {
            while (phonty.audMan.QueuedAudioIsPlaying)
                yield return null;
            phonty.audMan.QueueAudio(Phonty.audios.Get<SoundObject>("angry"), true);
            phonty.audMan.SetLoop(true);
            phonty.animator.SetDefaultAnimation("Chase", 1f);
            phonty.Navigator.SetSpeed(4f);
            phonty.Navigator.maxSpeed = 20f;
            yield break;
        }
    }
    public class Phonty_Dead : Phonty_StateBase
    {
        public Phonty_Dead(Phonty phonty) : base(phonty)
        {
        }

        public override void Enter()
        {
            base.Enter();
            base.ChangeNavigationState(new NavigationState_Disabled(phonty));
            phonty.angry = true;
            phonty.totalDisplay.SetActive(false);
            phonty.mapIcon.gameObject.SetActive(false);

            phonty.animator.Play("Idle", 1f);
            phonty.animator.SetDefaultAnimation("Idle", 1f);
        }
    }
}
