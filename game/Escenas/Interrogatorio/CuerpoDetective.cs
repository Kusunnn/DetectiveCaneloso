using Godot;
using System;

// Cuerpo visible de Caneloso (se pone como script de la instancia de player.tscn dentro del Jugador).
// Elige la animación según lo que hace el jugador y oculta el modelo en primera persona.
public partial class CuerpoDetective : Node3D
{
	public const string Reposo = "anim_reposo";
	public const string Caminar = "caminar_en_sitio";
	public const string Agacharse = "anim_agacharse";
	public const string Agarrar = "anim_agarrar";
	public const string Bailar = "anim_macarena";
	public const string Morir = "anim_muerte";

	private const float Mezcla = 0.2f;

	private AnimationPlayer _animaciones;
	// Animación de una sola vez (agarrar, bailar...) que se está reproduciendo; null si no hay.
	private string _accionActual;

	public bool HaciendoAccion => _accionActual != null;

	public override void _Ready()
	{
		foreach (Node nodo in FindChildren("*", "AnimationPlayer", true, false))
		{
			if (nodo is AnimationPlayer ap && ap.HasAnimation(Reposo))
			{
				_animaciones = ap;
				break;
			}
		}
		if (_animaciones == null)
		{
			GD.PushWarning("CuerpoDetective: no se encontró el AnimationPlayer con " + Reposo + ".");
			return;
		}

		PrepararCaminarEnSitio();
		_animaciones.GetAnimation(Reposo).LoopMode = Animation.LoopModeEnum.Linear;
		_animaciones.AnimationFinished += AlTerminarAnimacion;
		_animaciones.Play(Reposo);
	}

	// anim_caminar avanza la cadera hacia delante (root motion de Mixamo); como el movimiento
	// real lo hace el CharacterBody3D, se crea una copia que camina en el sitio y en bucle.
	private void PrepararCaminarEnSitio()
	{
		var original = _animaciones.GetAnimation("anim_caminar");
		if (original == null) return;

		var copia = (Animation)original.Duplicate();
		copia.LoopMode = Animation.LoopModeEnum.Linear;
		for (int pista = 0; pista < copia.GetTrackCount(); pista++)
		{
			if (copia.TrackGetType(pista) != Animation.TrackType.Position3D) continue;
			if (!copia.TrackGetPath(pista).ToString().Contains("Hips")) continue;

			float zInicial = copia.TrackGetKeyValue(pista, 0).AsVector3().Z;
			for (int clave = 0; clave < copia.TrackGetKeyCount(pista); clave++)
			{
				Vector3 posicion = copia.TrackGetKeyValue(pista, clave).AsVector3();
				copia.TrackSetKeyValue(pista, clave, new Vector3(posicion.X, posicion.Y, zInicial));
			}
		}
		_animaciones.GetAnimationLibrary("").AddAnimation(Caminar, copia);
	}

	// Tramos de anim_agacharse (medidos en la animación): bajar 0–1.2 s, agachado 1.2–5.2 s, levantarse 5.8 s–final.
	// El tramo central es un emote (mira a los lados, se balancea): agachado se congela en una sola pose.
	private const double FinBajar = 1.2;
	private const double FinAgachado = 5.2;
	private const double InicioLevantarse = 5.8;
	private const double PoseAgachado = 1.35;

	private bool _agachado = false;
	private bool _levantandose = false;

	// Llamar cada frame de física: reposo o caminar (de pie o agachado), salvo que haya una acción en curso.
	public void ActualizarMovimiento(bool moviendose)
	{
		if (_animaciones == null) return;

		// Moverse interrumpe cualquier acción (bailar, caerse...)
		if (moviendose) _accionActual = null;
		if (HaciendoAccion) return;

		if (_agachado || _levantandose)
		{
			ActualizarAgachado(moviendose);
			return;
		}

		string deseada = moviendose ? Caminar : Reposo;
		if (_animaciones.CurrentAnimation != deseada)
		{
			_animaciones.SpeedScale = 1f;
			_animaciones.Play(deseada, Mezcla);
		}
	}

	public void FijarAgachado(bool agachado)
	{
		if (_animaciones == null || agachado == _agachado) return;
		_agachado = agachado;
		_accionActual = null;
		if (agachado)
		{
			_levantandose = false;
			// Si estaba levantándose, se retoma desde la postura agachada sin saltos
			if (_animaciones.CurrentAnimation != Agacharse) _animaciones.Play(Agacharse, Mezcla);
			else if (_animaciones.CurrentAnimationPosition > FinAgachado) _animaciones.Seek(FinBajar, true);
		}
		else
		{
			_levantandose = true;
			if (_animaciones.CurrentAnimation != Agacharse) _animaciones.Play(Agacharse, Mezcla);
			double actual = _animaciones.CurrentAnimationPosition;
			// Desde la postura agachada se salta al tramo de levantarse; si aún bajaba, se invierte
			_animaciones.Seek(actual < FinBajar ? InicioLevantarse + (FinBajar - actual) * 0.5 : InicioLevantarse, true);
		}
	}

	private void ActualizarAgachado(bool moviendose)
	{
		if (_animaciones.CurrentAnimation != Agacharse)
		{
			if (_levantandose) { _levantandose = false; return; }
			_animaciones.Play(Agacharse, Mezcla);
		}
		double posicion = _animaciones.CurrentAnimationPosition;
		if (_agachado)
		{
			// Termina de bajar y se queda quieto en la pose agachada (antes se repetía el tramo
			// central, que es un emote, y parecía que Caneloso no paraba de hacer gestos).
			if (posicion >= PoseAgachado)
			{
				_animaciones.SpeedScale = 0f;
				if (posicion > PoseAgachado + 0.05) _animaciones.Seek(PoseAgachado, true);
			}
			else
			{
				_animaciones.SpeedScale = 1f;
			}
		}
		else
		{
			_animaciones.SpeedScale = 1.3f;
		}
	}

	// Reproduce una animación de una sola vez; al terminar vuelve a reposo.
	public void HacerAccion(string animacion)
	{
		if (_animaciones == null || !_animaciones.HasAnimation(animacion)) return;
		_accionActual = animacion;
		_animaciones.SpeedScale = 1f; // Agachado la animación está congelada (velocidad 0)
		_animaciones.Play(animacion, Mezcla);
	}

	private void AlTerminarAnimacion(StringName nombre)
	{
		// La muerte se queda en el último fotograma hasta que el jugador se mueva
		if (nombre == _accionActual && nombre != Morir)
		{
			_accionActual = null;
		}
		// Terminó de levantarse: vuelve a reposo
		if (nombre == Agacharse && _levantandose)
		{
			_levantandose = false;
			_animaciones.SpeedScale = 1f;
			_animaciones.Play(Reposo, Mezcla);
		}
	}

	// En primera persona el cuerpo no se dibuja, pero sigue proyectando sombra.
	public void MostrarEnPrimeraPersona(bool primeraPersona)
	{
		var modoSombra = primeraPersona
			? GeometryInstance3D.ShadowCastingSetting.ShadowsOnly
			: GeometryInstance3D.ShadowCastingSetting.On;
		foreach (Node nodo in FindChildren("*", "MeshInstance3D", true, false))
		{
			((MeshInstance3D)nodo).CastShadow = modoSombra;
		}
	}
}
