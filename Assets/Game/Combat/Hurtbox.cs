using UnityEngine;

namespace Lithostride.Combat
{
    public enum Team
    {
        Player = 0,
        Enemy = 1
    }

    /// <summary>
    /// Área que recebe dano (gatilho), separada do desenho e do corpo físico:
    /// o brilho de um efeito ou a asa do boss não contam como alvo. Aponta a
    /// <see cref="Health"/> que sofre o dano.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class Hurtbox : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private Team team;

        public Health Health => health;
        public Team Team => team;

        public void Configure(Health owner, Team side)
        {
            health = owner;
            team = side;
        }
    }
}
