using System;
using UnityEngine;

namespace Lithostride.Combat
{
    public enum AttackKind
    {
        /// <summary>Golpe corpo a corpo preso à mão.</summary>
        Melee = 0,

        /// <summary>Dispara um projétil no quadro ativo.</summary>
        Projectile = 1,

        /// <summary>Explosão num ponto à frente, no chão.</summary>
        Burst = 2
    }

    /// <summary>
    /// Um ataque do jogador, montado a partir de uma sequência de efeito do
    /// pack. O tempo sai dos quadros: antes de <see cref="ActiveFirst"/> é a
    /// antecipação, entre os ativos a hitbox vale, depois é a recuperação;
    /// só então conta a recarga.
    /// </summary>
    [Serializable]
    public sealed class AttackDefinition
    {
        [SerializeField] private string name;
        [SerializeField] private AttackKind kind;
        [SerializeField] private Sprite[] frames;
        [SerializeField, Min(1f)] private float framesPerSecond = 18f;
        [SerializeField, Min(0)] private int activeFirst;
        [SerializeField, Min(0)] private int activeLast;
        [SerializeField, Min(0f)] private float cooldown = 0.15f;
        [SerializeField, Min(0f)] private float damage = 10f;

        /// <summary>Hitbox em unidades, relativa aos pés, virado à direita.</summary>
        [SerializeField] private Vector2 hitboxOffset;
        [SerializeField] private Vector2 hitboxSize = Vector2.one;

        /// <summary>Centro do efeito relativo à mão (ou aos pés, na explosão), virado à direita.</summary>
        [SerializeField] private Vector2 effectOffset;

        /// <summary>A prancha foi desenhada virada para a esquerda: espelha quando o Kael olha para a direita.</summary>
        [SerializeField] private bool sheetFacesLeft;

        /// <summary>O quadro já traz a espada: a espada empunhada some durante o golpe.</summary>
        [SerializeField] private bool containsSword;

        /// <summary>Acerta o mesmo alvo mais de uma vez, a cada intervalo (0 = uma vez por golpe).</summary>
        [SerializeField, Min(0f)] private float multiHitInterval;

        /// <summary>Quadros desenhados atrás do corpo (lâmina atrás da cabeça na preparação). Nulo = todos na frente.</summary>
        [SerializeField] private bool[] behindBody;

        [SerializeField] private Vector2 knockback = new Vector2(4f, 3f);

        [SerializeField] private float projectileSpeed = 18f;
        [SerializeField] private float projectileLifetime = 1.1f;
        [SerializeField] private float projectileRadius = 0.4f;
        [SerializeField] private int flightFirst = 1;
        [SerializeField] private int flightLast = 4;

        public AttackDefinition(string name, AttackKind kind, Sprite[] frames, float framesPerSecond, int activeFirst,
            int activeLast, float cooldown, float damage, Vector2 hitboxOffset, Vector2 hitboxSize, Vector2 effectOffset,
            bool sheetFacesLeft, bool containsSword, float multiHitInterval, bool[] framesBehindBody = null)
        {
            behindBody = framesBehindBody;
            this.name = name;
            this.kind = kind;
            this.frames = frames;
            this.framesPerSecond = framesPerSecond;
            this.activeFirst = activeFirst;
            this.activeLast = activeLast;
            this.cooldown = cooldown;
            this.damage = damage;
            this.hitboxOffset = hitboxOffset;
            this.hitboxSize = hitboxSize;
            this.effectOffset = effectOffset;
            this.sheetFacesLeft = sheetFacesLeft;
            this.containsSword = containsSword;
            this.multiHitInterval = multiHitInterval;
        }

        /// <summary>Para o serializador da Unity.</summary>
        public AttackDefinition()
        {
        }

        public string Name => name;
        public AttackKind Kind => kind;
        public Sprite[] Frames => frames;
        public float FramesPerSecond => framesPerSecond;
        public int ActiveFirst => activeFirst;
        public int ActiveLast => activeLast;
        public float Cooldown => cooldown;
        public float Damage => damage;
        public Vector2 HitboxOffset => hitboxOffset;
        public Vector2 HitboxSize => hitboxSize;
        public Vector2 EffectOffset => effectOffset;
        public bool SheetFacesLeft => sheetFacesLeft;
        public bool ContainsSword => containsSword;
        public float MultiHitInterval => multiHitInterval;
        public bool BehindBody(int frame)
        {
            return behindBody != null && frame >= 0 && frame < behindBody.Length && behindBody[frame];
        }

        public Vector2 Knockback => knockback;
        public float ProjectileSpeed => projectileSpeed;
        public float ProjectileLifetime => projectileLifetime;
        public float ProjectileRadius => projectileRadius;
        public int FlightFirst => flightFirst;
        public int FlightLast => flightLast;

        /// <summary>Duração total da sequência (antecipação + ativo + recuperação).</summary>
        public float Duration => frames == null ? 0f : frames.Length / framesPerSecond;

        public float ActiveStart => activeFirst / framesPerSecond;
        public float ActiveEnd => (activeLast + 1) / framesPerSecond;
    }
}
