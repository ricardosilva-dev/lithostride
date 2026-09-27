using UnityEngine;

namespace Lithostride.Player
{
    /// <summary>Ícone que sobe e some (energia do minério quebrado). Só apresentação.</summary>
    public sealed class FloatingIcon : MonoBehaviour
    {
        private const float Lifetime = 0.9f;

        private SpriteRenderer iconRenderer;
        private float age;
        private Vector3 start;

        public static void Spawn(Sprite icon, Vector3 position)
        {
            GameObject item = new GameObject("Energia");
            item.transform.position = position;
            SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
            renderer.sprite = icon;
            renderer.sortingOrder = 30;
            item.AddComponent<FloatingIcon>().iconRenderer = renderer;
        }

        private void Start()
        {
            start = transform.position;
        }

        private void Update()
        {
            age += Time.deltaTime;
            float t = age / Lifetime;
            transform.position = start + (Vector3.up * (1.2f * t));
            iconRenderer.color = new Color(1f, 1f, 1f, 1f - (t * t));
            if (age >= Lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}
