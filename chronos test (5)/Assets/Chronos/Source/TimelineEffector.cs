﻿using System.Collections.Generic;
using UnityEngine;

namespace Chronos
{
	/// <summary>
	/// An internal base class that applies time effects from a timeline to most built-in Unity components.
	/// </summary>
	public abstract class TimelineEffector : MonoBehaviour
	{
		protected internal const float DefaultRecordingInterval = 0.5f;

		public TimelineEffector()
		{
			components = new HashSet<IComponentTimeline>();
			audioSources = new List<AudioSourceTimeline>();
		}

		protected abstract Timeline timeline { get; }

		protected virtual void Awake()
		{
			CacheComponents();
		}

		protected virtual void Start()
		{
			foreach (IComponentTimeline component in components)
			{
				component.AdjustProperties();
				component.Start();
			}
		}

		protected virtual void Update()
		{
			foreach (IComponentTimeline component in components)
			{
				component.Update();
			}
		}

		protected virtual void FixedUpdate()
		{
			foreach (IComponentTimeline component in components)
			{
				component.FixedUpdate();
			}
		}

		#region Rewinding / Recording
		
		[SerializeField]
		private float _recordingInterval = DefaultRecordingInterval;

		/// <summary>
		/// The interval in seconds at which snapshots will be recorder. Lower values offer more rewind precision but require more memory. 
		/// </summary>
		public float recordingInterval
		{
			get { return _recordingInterval; }
			set
			{
				_recordingInterval = value;

				if (recorder != null)
				{
					recorder.Reset();
				}
			}
		}

		#endregion

		#region Components

		internal HashSet<IComponentTimeline> components;

		public new AnimationTimeline animation { get; protected set; }
		public AnimatorTimeline animator { get; protected set; }
		public List<AudioSourceTimeline> audioSources { get; protected set; }
		public AudioSourceTimeline audioSource { get; protected set; }
		public NavMeshAgentTimeline navMeshAgent { get; protected set; }
		public new IParticleSystemTimeline particleSystem { get; protected set; }
		public new RigidbodyTimeline3D rigidbody { get; protected set; }
		public new RigidbodyTimeline2D rigidbody2D { get; protected set; }
		public new TransformTimeline transform { get; protected set; }
		public WindZoneTimeline windZone { get; protected set; }

		protected IRecorder recorder
		{
			get
			{
				if (rigidbody != null) return rigidbody;
				if (rigidbody2D != null) return rigidbody2D;
				if (transform != null) return transform;
				return null;
			}
		}
		
		/// <summary>
		/// The components used by the timeline are cached for performance. If you add or remove built-in Unity components on the GameObject, you need to call this method to update the timeline accordingly. 
		/// </summary>
		public virtual void CacheComponents()
		{
			components.Clear();

			// Animator

			var animatorComponent = GetComponent<Animator>();

			if (animator == null && animatorComponent != null)
			{
				animator = new AnimatorTimeline(timeline, animatorComponent);
				components.Add(animator);
			}
			else if (animator != null && animatorComponent == null)
			{
				animator = null;
			}

			// Animation

			var animationComponent = GetComponent<Animation>();

			if (animation == null && animationComponent != null)
			{
				animation = new AnimationTimeline(timeline, animationComponent);
				components.Add(animation);
			}
			else if (animation != null && animationComponent == null)
			{
				animation = null;
			}

			// AudioSources

			var audioSourceComponents = GetComponents<AudioSource>();

			// Remove timelines for absent components
			for (int i = 0; i < audioSources.Count; i++)
			{
				var audioSource = audioSources[i];

				bool audioSourceComponentExists = false;

				for (int j = 0; j < audioSourceComponents.Length; j++)
				{
					var audioSourceComponent = audioSourceComponents[j];

					if (audioSource.component == audioSourceComponent)
					{
						audioSourceComponentExists = true;
						break;
					}
				}

				if (!audioSourceComponentExists)
				{
					audioSources.Remove(audioSource);
				}
			}

			// Add timelines for new components
			for (int i = 0; i < audioSourceComponents.Length; i++)
			{
				var audioSourceComponent = audioSourceComponents[i];

				bool audioSourceExists = false;

				for (int j = 0; j < audioSources.Count; j++)
				{
					var audioSource = audioSources[j];

					if (audioSource.component == audioSourceComponent)
					{
						audioSourceExists = true;
						break;
					}
				}

				if (!audioSourceExists)
				{
					var newAudioSource = new AudioSourceTimeline(timeline, audioSourceComponent);
					audioSources.Add(newAudioSource);
					components.Add(newAudioSource);
				}
			}

			this.audioSource = audioSources.Count > 0 ? audioSources[0] : null;

			// NavMeshAgent

			var navMeshAgentComponent = GetComponent<NavMeshAgent>();

			if (navMeshAgent == null && navMeshAgentComponent != null)
			{
				navMeshAgent = new NavMeshAgentTimeline(timeline, navMeshAgentComponent);
				components.Add(navMeshAgent);
			}
			else if (animation != null && navMeshAgentComponent == null)
			{
				navMeshAgent = null;
			}

			// ParticleSystem

			var particleSystemComponent = GetComponent<ParticleSystem>();

			if (particleSystem == null && particleSystemComponent != null)
			{
				if (timeline.rewindable)
				{
					particleSystem = new RewindableParticleSystemTimeline(timeline, particleSystemComponent);
				}
				else
				{
					particleSystem = new NonRewindableParticleSystemTimeline(timeline, particleSystemComponent);
				}

				components.Add(particleSystem);
			}
			else if (particleSystem != null && particleSystemComponent == null)
			{
				particleSystem = null;
			}

			// WindZone

			var windZoneComponent = GetComponent<WindZone>();

			if (windZone == null && windZoneComponent != null)
			{
				windZone = new WindZoneTimeline(timeline, windZoneComponent);
				components.Add(windZone);
			}
			else if (windZone != null && windZoneComponent == null)
			{
				windZone = null;
			}

			// Only activate one of Rigidbody / Rigidbody2D / Transform timelines at once
			
			var rigidbodyComponent = GetComponent<Rigidbody>();
			var rigidbody2DComponent = GetComponent<Rigidbody2D>();
			var transformComponent = GetComponent<Transform>();

			if (rigidbody == null && rigidbodyComponent != null)
			{
				rigidbody = new RigidbodyTimeline3D(timeline, rigidbodyComponent);
				components.Add(rigidbody);
				rigidbody2D = null;
				transform = null;
			}
			else if (rigidbody2D == null && rigidbody2DComponent != null)
			{
				rigidbody2D = new RigidbodyTimeline2D(timeline, rigidbody2DComponent);
				components.Add(rigidbody2D);
				rigidbody = null;
				transform = null;
			}
			else if (transform == null)
			{
				transform = new TransformTimeline(timeline, transformComponent);
				components.Add(transform);
				rigidbody = null;
				rigidbody2D = null;
			}
		}

		/// <summary>
		/// Resets the saved snapshots. 
		/// </summary>
		public void ResetRecording()
		{
			recorder.Reset();
		}

		/// <summary>
		/// Estimate the memory usage in bytes from the storage of snapshots for the current recording duration and interval. 
		/// </summary>
		public int EstimateMemoryUsage()
		{
			if (Application.isPlaying)
			{
				if (recorder == null)
				{
					return 0;
				}

				return recorder.EstimateMemoryUsage();
			}
			else
			{
				var timeline = GetComponentInParent<Timeline>();

				if (!timeline.rewindable)
				{
					return 0;
				}

				if (GetComponent<Rigidbody>() != null)
				{
					return RigidbodyTimeline3D.EstimateMemoryUsage(timeline.recordingDuration, recordingInterval);
				}
				else if (GetComponent<Rigidbody2D>() != null)
				{
					return RigidbodyTimeline2D.EstimateMemoryUsage(timeline.recordingDuration, recordingInterval);
				}
				else if (GetComponent<Transform>() != null)
				{
					return TransformTimeline.EstimateMemoryUsage(timeline.recordingDuration, recordingInterval);
				}
				else
				{
					return 0;
				}
			}
		}

		#endregion
	}
}
