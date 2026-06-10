using UnityEngine;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// uGUI прицел TPS.
    /// Живёт в TPS-блоке HudController — активируется/деактивируется вместе с блоком,
    /// дополнительной логики переключения не требует.
    /// Визуал строится из 4 дочерних Image-полосок через Forge.
    /// </summary>
    public sealed class CrosshairUI : MonoBehaviour
    {
        // Компонент-маркер. Вся логика видимости — на родительском TPS-блоке.
        // Расширяется в M8 (анимация разброса, индикаторы).
    }
}
