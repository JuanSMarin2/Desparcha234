using UnityEngine;
using System.Collections.Generic;

public enum MarblePowerType { None, MorePower, Unmovable, Ghost }

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class MarblePower : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D col;
    [SerializeField] private int playerIndex; // Índice de jugador: 0..3

    [Header("Ajustes de poderes")]
    [SerializeField] private float morePowerMultiplier = 1.5f;
    [SerializeField] private int morePowerTurns = 3;
    [SerializeField] private int unmovableTurns = 2;
    [SerializeField] private int ghostTurns = 2;

    [Header("Colisiones / Capas")]
    [SerializeField] private LayerMask wallMask;           // Layer de muros
    [SerializeField] private string normalMarbleLayer = "Marble";
    [SerializeField] private string ghostMarbleLayer = "GhostMarble";

    [Header("Desencajar de pared")]
    [SerializeField] private float unstickRadius = 0.2f;   // radio base por anillo
    [SerializeField] private int unstickRings = 6;         // cantidad de anillos

    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Color colorNone = Color.white;
    [SerializeField] private Color colorMorePow = new Color(1f, 0.55f, 0f); // naranja
    [SerializeField] private Color colorUnmov = Color.gray;               // gris
    [SerializeField] private Color colorGhost = new Color(0.5f, 0f, 0.7f); // morado
    [SerializeField, Range(0f, 1f)] private float ghostAlpha = 0.6f;

 

    // Estado actual (patrón State)
    private IMarblePowerState state;

    public IMarblePowerState CurrentState => state;
    public MarblePowerType CurrentType => state?.Type ?? MarblePowerType.None;
    public int CurrentTurnsLeft => state?.TurnsLeft ?? 0;

    // Gestión de colisiones entre canicas (para utilidades)
    private static readonly List<Collider2D> allMarbleColliders = new();

    // ===== Ciclo de vida =====

    void Reset() { rb = GetComponent<Rigidbody2D>(); col = GetComponent<Collider2D>(); sr = GetComponentInChildren<SpriteRenderer>(); }

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!col) col = GetComponent<Collider2D>();

        if (!sr) sr = GetComponentInChildren<SpriteRenderer>();


        if (!allMarbleColliders.Contains(col))
            allMarbleColliders.Add(col);

        SetState(new NoneState(this));
    }

    private void SetMarbleColor(Color c, float? aOverride = null)
    {
        if (!sr) return;
        var c2 = c;
        if (aOverride.HasValue) c2.a = aOverride.Value;
        sr.color = c2;
    }

    void OnDestroy()
    {
        allMarbleColliders.Remove(col);
    }

    void Update()
    {
        // Avisar al estado si es / no es mi turno (para habilitar efectos por turno)
        bool isMyTurn = (TurnManager.instance != null &&
                         TurnManager.instance.GetCurrentPlayerIndex() == playerIndex);
        state?.OnTurnBecameCurrent(isMyTurn);
    }

    // Llamar desde tu flujo cuando TERMINE el turno de ESTA canica
    public void NotifyTurnEndedIfMine()
    {
        if (TurnManager.instance != null &&
            TurnManager.instance.GetCurrentPlayerIndex() == playerIndex)
        {
            state?.OnTurnEnded();
        }
    }

    // Aplicar poder externamente (pickup, UI, etc.)
    public void ApplyPower(MarblePowerType type)
    {
        switch (type)
        {
            case MarblePowerType.MorePower:
                SetState(new MorePowerState(this, morePowerMultiplier, morePowerTurns));
                break;

            case MarblePowerType.Unmovable:
                SetState(new UnmovableState(this, unmovableTurns));
                break;

            case MarblePowerType.Ghost:
                SetState(new GhostState(this, ghostTurns));
                break;

            default:
                SetState(new NoneState(this));
                break;
        }
    }

    // Usado por MarbleShooter2D para multiplicar fuerza de disparo
    public float GetLaunchMultiplier(float baseMultiplier)
    {
        return state?.GetLaunchMultiplier(baseMultiplier) ?? baseMultiplier;
    }

    // ===== Helpers internos =====

    private void SetState(IMarblePowerState newState)
    {
        state?.OnExit();
        state = newState;
        state?.OnEnter();
    }

    // Ignorar empujones (colisiones) entre canicas (no usado en la versión con Kinematic)
    public void SetIgnoreOtherMarbles(bool ignore)
    {
        if (!col) return;
        foreach (var other in allMarbleColliders)
        {
            if (!other || other == col) continue;
            Physics2D.IgnoreCollision(col, other, ignore);
        }
    }

    // Cambiar layer para ignorar muros mediante Collision Matrix
    public void SetIgnoreWalls(bool ignore)
    {
        int targetLayer = LayerMask.NameToLayer(ignore ? ghostMarbleLayer : normalMarbleLayer);
        if (targetLayer >= 0) gameObject.layer = targetLayer;
    }

    // Volver Dynamic/Kinematic (clave para Inmovible con rebote)
    public void SetBodyKinematic(bool kinematic, bool zeroVelocity = false)
    {
        if (!rb) return;

        if (kinematic)
        {
            if (zeroVelocity)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        else
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
    }

    // Si termina Ghost y quedé en pared, buscar punto libre cercano
    public void UnstickFromWallsIfNeeded()
    {
        if (!rb || !col) return;

        // ¿Estoy solapando con un muro ahora?
        if (Physics2D.OverlapCircle(rb.position, Mathf.Max(col.bounds.extents.x, col.bounds.extents.y), wallMask))
        {
            Vector2 center = rb.position;

            for (int ring = 1; ring <= unstickRings; ring++)
            {
                int samples = 12 + ring * 6;
                float radius = unstickRadius * ring;

                for (int s = 0; s < samples; s++)
                {
                    float a = (Mathf.PI * 2f) * (s / (float)samples);
                    Vector2 candidate = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;

                    if (!Physics2D.OverlapCircle(candidate, Mathf.Max(col.bounds.extents.x, col.bounds.extents.y), wallMask))
                    {
                        rb.position = candidate;
                        rb.linearVelocity = Vector2.zero;
                        rb.angularVelocity = 0f;
                        return;
                    }
                }
            }
        }
    }

    // ====== Interfaces / Estados ======

    public interface IMarblePowerState
    {
        MarblePowerType Type { get; }
        int TurnsLeft { get; }
        void OnEnter();
        void OnExit();
        void OnTurnEnded();                          // descontar turnos al acabar mi turno
        float GetLaunchMultiplier(float baseMult);   // ajustar fuerza de disparo
        void OnTurnBecameCurrent(bool isCurrentTurn);
    }

    // ---- Base sin poder ----
    private class NoneState : IMarblePowerState
    {
        protected readonly MarblePower ctx;

        public virtual MarblePowerType Type => MarblePowerType.None;
        public virtual int TurnsLeft => 0;

        public NoneState(MarblePower ctx) { this.ctx = ctx; }

        public virtual void OnEnter()
        {
            ctx.SetIgnoreOtherMarbles(false);
            ctx.SetIgnoreWalls(false);
            ctx.SetBodyKinematic(false);
            ctx.SetMarbleColor(ctx.colorNone);
        }

        public virtual void OnExit() { }
        public virtual void OnTurnEnded() { }
        public virtual float GetLaunchMultiplier(float baseMult) => baseMult;

        public virtual void OnTurnBecameCurrent(bool isCurrentTurn)
        {
            // Sin poder: comportamiento normal
        }
    }

    // ---- Más Potencia ----
    private class MorePowerState : NoneState
    {
        private int turns;
        private readonly float mult;

        public override MarblePowerType Type => MarblePowerType.MorePower;
        public override int TurnsLeft => turns;

        public MorePowerState(MarblePower ctx, float mult, int turns) : base(ctx)
        {
            this.mult = mult;
            this.turns = Mathf.Max(1, turns);
        }

        public override float GetLaunchMultiplier(float baseMult) => baseMult * mult;

        public override void OnTurnEnded()
        {
            turns--;
            if (turns <= 0) ctx.SetState(new NoneState(ctx));
        }

        public override void OnEnter()
        {
            base.OnEnter();
            ctx.SetMarbleColor(ctx.colorMorePow);
        }
        public override void OnExit()
        {
            ctx.SetMarbleColor(ctx.colorNone);
        }
    }

    // ---- Inmovible (rebota pero no se mueve) ----
    private class UnmovableState : NoneState
    {
        private int turns;

        public override MarblePowerType Type => MarblePowerType.Unmovable;
        public override int TurnsLeft => turns;

        public UnmovableState(MarblePower ctx, int turns) : base(ctx)
        {
            this.turns = Mathf.Max(1, turns);
        }

        public override void OnEnter()
        {
            base.OnEnter();
            ctx.SetMarbleColor(ctx.colorUnmov);
            bool isMyTurn = (TurnManager.instance && TurnManager.instance.GetCurrentPlayerIndex() == ctx.playerIndex);
            ctx.SetBodyKinematic(!isMyTurn, zeroVelocity: !isMyTurn);
        }
        public override void OnExit()
        {
            ctx.SetBodyKinematic(false);
            ctx.SetMarbleColor(ctx.colorNone);
        }

        public override void OnTurnBecameCurrent(bool isCurrentTurn)
        {
            // En mi turno: Dynamic para poder ser lanzado
            // Fuera de mi turno: Kinematic para que no me empujen
            ctx.SetBodyKinematic(!isCurrentTurn, zeroVelocity: !isCurrentTurn);
        }

        public override void OnTurnEnded()
        {
            turns--;
            if (turns <= 0)
            {
                ctx.SetBodyKinematic(false);
                ctx.SetState(new NoneState(ctx));
            }
            else
            {
                // Sigo con poder: fuera de mi turno quedo Kinematic
                ctx.SetBodyKinematic(true, zeroVelocity: true);
            }
        }

        
    }

    // ---- Fantasma (atraviesa muros en su turno) ----
    private class GhostState : NoneState
    {
        private int turns;

        public override MarblePowerType Type => MarblePowerType.Ghost;
        public override int TurnsLeft => turns;

        public GhostState(MarblePower ctx, int turns) : base(ctx)
        {
            this.turns = Mathf.Max(1, turns);
        }

        public override void OnEnter()
        {
            base.OnEnter();
            // Por defecto, no fantasma fuera de turno
            ctx.SetMarbleColor(ctx.colorGhost, ctx.ghostAlpha);

            ctx.SetIgnoreWalls(false);

            // Si justo es mi turno al entrar, activo fantasma
            bool isMyTurn = (TurnManager.instance &&
                             TurnManager.instance.GetCurrentPlayerIndex() == ctx.playerIndex);
            ctx.SetIgnoreWalls(isMyTurn);
        }

        public override void OnTurnBecameCurrent(bool isCurrentTurn)
        {
            // Solo en mi turno atravieso muros
            ctx.SetIgnoreWalls(isCurrentTurn);

            ctx.SetMarbleColor(ctx.colorGhost, ctx.ghostAlpha);
        }

        public override void OnTurnEnded()
        {
            turns--;
            // Al terminar mi turno, desactivo fantasma y me “desencajo” si quedé en pared
            ctx.SetIgnoreWalls(false);
            ctx.UnstickFromWallsIfNeeded();

            if (turns <= 0)
            {
                ctx.SetState(new NoneState(ctx));
            }
        }

        public override void OnExit()
        {
            ctx.SetIgnoreWalls(false);
            ctx.SetMarbleColor(ctx.colorNone);
        }
    }
}
