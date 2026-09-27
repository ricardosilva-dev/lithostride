using UnityEngine;

namespace Lithostride.Combat
{
    /// <summary>
    /// Dano por encostar (corpo do boss, investida). Gatilho que fere
    /// hurtboxes do outro time enquanto ativo; a invulnerabilidade da
    /// <see cref="Health"/> impede dano por quadro.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class ContactDamage : MonoBehaviour
    {
        [SerializeField] private Team team = Team.Enemy;
        [SerializeField, Min(0f)] private float damage = 10f;

        public bool Active { get; set; } = true;

        public float Damage
        {
            get => damage;
            set => damage = value;
        }

        public void Configure(Team side, float amount)
        {
            team = side;
            damage = amount;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!Active)
            {
                return;
            }

            Hurtbox hurtbox = other.GetComponent<Hurtbox>();
            if (hurtbox != null && hurtbox.Team != team && hurtbox.Health != null && !hurtbox.Health.IsDead)
            {
                hurtbox.Health.TakeDamage(damage, transform.position);
            }
        }
    }
}
