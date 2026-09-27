using System.Collections.Generic;
using UnityEngine;

namespace Lithostride.Combat
{
    /// <summary>
    /// Lista de efeitos e projéteis vivos na cena. Reiniciar a luta, renascer
    /// ou resetar o teste apaga todos: nada fica acumulando.
    /// </summary>
    public sealed class EffectRegistry : MonoBehaviour
    {
        private readonly List<GameObject> alive = new List<GameObject>();

        public int Count
        {
            get
            {
                alive.RemoveAll(item => item == null);
                return alive.Count;
            }
        }

        public void Track(GameObject item)
        {
            if (item != null)
            {
                alive.Add(item);
            }
        }

        public void ClearAll()
        {
            foreach (GameObject item in alive)
            {
                if (item != null)
                {
                    Destroy(item);
                }
            }

            alive.Clear();
        }
    }
}
