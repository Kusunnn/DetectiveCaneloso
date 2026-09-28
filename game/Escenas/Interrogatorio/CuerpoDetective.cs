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

	// Llamar cada frame de física: reposo o caminar, salvo que haya una acción en curso.
	public void ActualizarMovimiento(bool moviendose)
	{
		if (_animaciones == null) return;

		// Moverse interrumpe cualquier acción (bailar, agacharse...)
		if (moviendose) _accionActual = null;
		if (HaciendoAccion) return;

		string deseada = moviendose ? Caminar : Reposo;
		if (_animaciones.CurrentAnimation != deseada)
		{
			_animaciones.Play(deseada, Mezcla);
		}
	}

	// Reproduce una animación de una sola vez; al terminar vuelve a reposo.
	public void HacerAccion(string animacion)
	{
		if (_animaciones == null || !_animaciones.HasAnimation(animacion)) return;
		_accionActual = animacion;
		_animaciones.Play(animacion, Mezcla);
	}

	private void AlTerminarAnimacion(StringName nombre)
	{
		// La muerte se queda en el último fotograma hasta que el jugador se mueva
		if (nombre == _accionActual && nombre != Morir)
		{
			_accionActual = null;
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
