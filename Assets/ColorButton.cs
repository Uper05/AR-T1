using UnityEngine;
using UnityEngine.UI;

// Coloque num botão da UI. Defina a cor desse botão e a referência do ObjectColorSelector no Inspector.
[RequireComponent(typeof(Button))]
public class ColorButton : MonoBehaviour
{
    [SerializeField] private Color color = Color.white;
    [SerializeField] private ObjectColorSelector selector;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => selector.OnColorButtonPressed(color));

        Image img = GetComponent<Image>();
        if (img != null)
            img.color = color; // o botão já mostra a cor que representa
    }
}
