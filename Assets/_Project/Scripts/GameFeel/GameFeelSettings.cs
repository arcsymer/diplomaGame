using UnityEngine;

namespace DiplomaGame.Runtime.GameFeel
{
    /// <summary>
    /// ScriptableObject с константами game-feel героя.
    /// Создаётся через меню или ForgeBatch.SetupGameFeel().
    /// </summary>
    [CreateAssetMenu(
        menuName = "DiplomaGame/GameFeel/GameFeelSettings",
        fileName = "GameFeelSettings")]
    public sealed class GameFeelSettings : ScriptableObject
    {
        // ----------------------------------------------------------------
        // Рекойл камеры
        // ----------------------------------------------------------------

        [Header("Recoil Camera")]
        [Tooltip("Амплитуда импульса рекойла при выстреле.")]
        public float shotImpulseAmplitude = 0.08f;

        [Tooltip("Скорость экспоненциального затухания рекойла (1/с).")]
        public float shotImpulseDecay = 8f;

        [Tooltip("Вариация питча звука выстрела (±variance).")]
        public float shotPitchVariance = 0.12f;

        [Tooltip("Множитель рекойла во время Overcharge.")]
        public float overchargeRecoilMult = 1.3f;

        // ----------------------------------------------------------------
        // Shockwave shake
        // ----------------------------------------------------------------

        [Header("Shockwave")]
        [Tooltip("Амплитуда импульса шейка от Shockwave.")]
        public float shockwaveImpulseAmplitude = 0.35f;

        [Tooltip("Скорость затухания шейка от Shockwave.")]
        public float shockwaveImpulseDecay = 5f;

        // ----------------------------------------------------------------
        // HitFlash
        // ----------------------------------------------------------------

        [Header("HitFlash")]
        [Tooltip("Длительность белой вспышки при уроне (с).")]
        public float hitFlashDuration = 0.07f;

        [Tooltip("Длительность зелёного флэша при лечении (с).")]
        public float repairFlashDuration = 0.10f;

        [Tooltip("Цвет флэша при лечении.")]
        public Color repairFlashColor = new Color(0.2f, 1f, 0.3f);

        // ----------------------------------------------------------------
        // Knockback
        // ----------------------------------------------------------------

        [Header("Knockback")]
        [Tooltip("Дистанция нокбэка пехоты при уроне (м).")]
        public float knockbackDistance = 0.25f;

        [Tooltip("Включить/выключить нокбэк.")]
        public bool knockbackEnabled = true;

        // ----------------------------------------------------------------
        // Hitstop
        // ----------------------------------------------------------------

        [Header("Hitstop")]
        [Tooltip("Длительность hitstop (с).")]
        public float hitstopDuration = 0.06f;

        [Tooltip("Целевой timeScale во время hitstop (дробная часть).")]
        public float hitstopTargetScale = 0.05f;

        [Tooltip("Включить/выключить hitstop.")]
        public bool hitstopEnabled = true;

        // ----------------------------------------------------------------
        // Dash Trail
        // ----------------------------------------------------------------

        [Header("Dash Trail")]
        [Tooltip("Длительность видимости dash-трейла (с).")]
        public float dashTrailDuration = 0.25f;

        // ----------------------------------------------------------------
        // Hitmarker (Circle-20)
        // ----------------------------------------------------------------

        [Header("Hitmarker (Circle-20)")]
        [Tooltip("Цвет вспышки хитмаркера при попадании (тёплый оранжевый).")]
        public Color hitmarkerColorHit = new Color(1f, 0.55f, 0f, 1f);

        [Tooltip("Масштаб раскрытия крестика при попадании (1.0 → hitmarkerExpandScale → 1.0).")]
        public float hitmarkerExpandScale = 1.15f;

        [Tooltip("Масштаб раскрытия крестика при промахе (1.0 → hitmarkerMissScale → 1.0).")]
        public float hitmarkerMissScale = 1.05f;

        [Tooltip("Длительность анимации хитмаркера (с).")]
        public float hitmarkerDuration = 0.10f;
    }
}
