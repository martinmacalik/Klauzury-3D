using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DummyEnemy : MonoBehaviour, IDamageable
{
    [SerializeField] int hp = 5;
    [SerializeField] bool destroyOnDeath = true;

    public void TakeDamage(int amount)
    {
        hp -= Mathf.Max(0, amount);
        // quick visual: flash color if it has a renderer
        var rend = GetComponentInChildren<Renderer>();
        if (rend) StartCoroutine(Flash(rend));
        if (hp <= 0 && destroyOnDeath) Destroy(gameObject);
    }

    System.Collections.IEnumerator Flash(Renderer r)
    {
        var mat = r.material;
        Color orig = mat.HasProperty("_Color") ? mat.color : Color.white;
        if (mat.HasProperty("_Color")) mat.color = Color.red;
        yield return new WaitForSeconds(0.05f);
        if (mat.HasProperty("_Color")) mat.color = orig;
    }
}