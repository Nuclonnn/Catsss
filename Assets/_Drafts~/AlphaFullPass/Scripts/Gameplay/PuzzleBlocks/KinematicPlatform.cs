using Catsss.Player;
using Unity.Netcode;
using UnityEngine;

namespace Catsss.Gameplay.PuzzleBlocks
{
    public sealed class KinematicPlatform : NetworkBehaviour
    {
        public enum ActivationMode
        {
            Cycle,
            SignalDriven,
            Resonance
        }

        [SerializeField] private ActivationMode activationMode;
        [SerializeField] private Transform pointA;
        [SerializeField] private Transform pointB;
        [SerializeField, Min(0.01f)] private float moveDuration = 2f;
        [SerializeField] private int requiredChargeId;

        private float _progress;
        private int _direction = 1;
        private bool _isPlaying;
        private int _resonantPlayers;

        private void Update()
        {
            if (!IsServer || pointA == null || pointB == null)
            {
                return;
            }

            if (activationMode == ActivationMode.Cycle)
            {
                _isPlaying = true;
            }

            if (!_isPlaying)
            {
                return;
            }

            _progress += _direction * Time.deltaTime / moveDuration;

            if (activationMode == ActivationMode.Cycle)
            {
                if (_progress >= 1f || _progress <= 0f)
                {
                    _progress = Mathf.Clamp01(_progress);
                    _direction *= -1;
                }
            }
            else
            {
                _progress = Mathf.Clamp01(_progress);
            }

            transform.position = Vector3.Lerp(pointA.position, pointB.position, Mathf.SmoothStep(0f, 1f, _progress));
            transform.rotation = Quaternion.Slerp(pointA.rotation, pointB.rotation, Mathf.SmoothStep(0f, 1f, _progress));

            if (activationMode != ActivationMode.Cycle && (_progress <= 0f || _progress >= 1f))
            {
                _isPlaying = false;
            }
        }

        public void PlayForward()
        {
            if (!IsServer)
            {
                return;
            }

            _direction = 1;
            _isPlaying = true;
        }

        public void PlayReverse()
        {
            if (!IsServer)
            {
                return;
            }

            _direction = -1;
            _isPlaying = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer || activationMode != ActivationMode.Resonance)
            {
                return;
            }

            PlayerChargeHolder chargeHolder = other.GetComponentInParent<PlayerChargeHolder>();
            if (chargeHolder != null && chargeHolder.CurrentChargeId == requiredChargeId)
            {
                _resonantPlayers++;
                PlayForward();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsServer || activationMode != ActivationMode.Resonance)
            {
                return;
            }

            PlayerChargeHolder chargeHolder = other.GetComponentInParent<PlayerChargeHolder>();
            if (chargeHolder != null && chargeHolder.CurrentChargeId == requiredChargeId)
            {
                _resonantPlayers = Mathf.Max(0, _resonantPlayers - 1);
                if (_resonantPlayers == 0)
                {
                    PlayReverse();
                }
            }
        }
    }
}
