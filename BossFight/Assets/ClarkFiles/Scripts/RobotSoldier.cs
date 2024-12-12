using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


namespace ccomstock
{
    public enum EnemyStates
    {
        idle, pursue, melee, ranged, dead
    }
    public class RobotSoldier : MonoBehaviour
    {
        [SerializeField] float speed;
        [SerializeField] GameObject meleeWeapon;
        [SerializeField] AudioClip shovelSwingSound;
        [SerializeField] GameObject rocket;
        [SerializeField] Transform rocketSpawn;
        [SerializeField] AudioClip spawnVoiceline;
        [SerializeField] AudioClipCollection voiceLines;

        Navigator navigator;
        Transform _transform;
        Transform player;
        Rigidbody _rigidbody;

        EnemyStates state = EnemyStates.idle;
        float currentStateElapsed = 0;
        Vector3 currentTargetNodePosition;
        int pathNodeIndex = 0;
        Vector3 targetVelocity;
        bool inMeleeRange = false;
        float distance = 0;

        // Start is called before the first frame update
        void Start()
        {
            navigator = GetComponent<Navigator>();
            player = FindObjectOfType<PlayerLogic>().transform;
            _rigidbody = GetComponent<Rigidbody>();
            _transform = transform;
            SoundEffectsManager.instance.PlayAudioClip(spawnVoiceline, true);
            StartCoroutine(SayRandomVoiceLines());
        }

        // Update is called once per frame
        void Update()
        {
            currentStateElapsed += Time.deltaTime;
            distance = Vector3.Distance(_transform.position, player.position);

            switch (state)
            {
                case EnemyStates.idle:
                    UpdateIdle();
                    break;
                case EnemyStates.melee:
                    UpdateMelee();
                    break;
                case EnemyStates.pursue:
                    UpdatePursue();
                    break;
                case EnemyStates.ranged:
                    UpdateRanged();
                    break;
                case EnemyStates.dead:
                    UpdateDead();
                    break;
            }
        }

        private void FixedUpdate()
        {
            _rigidbody.velocity = targetVelocity;
        }

        void UpdateIdle()
        {
            //Debug.Log("in idle");

            if (currentStateElapsed > 1.0f)
            {
                if (inMeleeRange)
                    EnterMelee();
                else
                    AttemptBeginPursue();
            }
        }

        bool AttemptBeginPursue()
        {
            //Debug.Log("attempting to pursue");

            if (AttemptMakePathToPlayer())
            {
                if (distance >= 10f)
                {
                    StartCoroutine(RangedAttack());
                }
                pathNodeIndex = 0;
                state = EnemyStates.pursue;
                currentStateElapsed = 0;

                return true;
            }

            Debug.Log("failed attempt to pursue");

            return false;
        }

        void UpdatePursue()
        {
            //Debug.Log("in pursue");

            currentTargetNodePosition = navigator.PathNodes[pathNodeIndex];

            //Debug.Log("current target position is " + currentTargetNodePosition + " at index " + pathNodeIndex);

            Vector3 dirToNode = (currentTargetNodePosition - _transform.position);//.normalized;
            dirToNode.y = 0;
            dirToNode.Normalize();

            _transform.forward = dirToNode;

            float distToNode = Vector3.Distance(currentTargetNodePosition, _transform.position);

            //Debug.Log("distance to node: " + distToNode);

            if (distToNode < 3f)
            {
                //Debug.Log("close to node");
                pathNodeIndex++;

                if (pathNodeIndex >= navigator.PathNodes.Count)
                {
                    pathNodeIndex = 0;
                    AttemptMakePathToPlayer();
                    return;
                }

            }

            if (inMeleeRange)
            {
                // do melee attack
                EnterMelee();
                return;
            }

            targetVelocity = _transform.forward * speed;
            targetVelocity.y = _rigidbody.velocity.y;

            if (currentStateElapsed > 1) // rebuild path every half second
            {
                pathNodeIndex = 1;
                AttemptMakePathToPlayer();
            }
        }

        void EnterMelee()
        {
            //Debug.Log("Enter melee");
            // animator.setTrigger("melee");
            var dirToPlayer = (player.transform.position - transform.position).normalized;
            dirToPlayer.y = 0;
            transform.forward = dirToPlayer;
            targetVelocity = Vector3.zero;
            state = EnemyStates.melee;
            currentStateElapsed = 0;

            StartCoroutine(HandleMelee());
        }

        IEnumerator HandleMelee()
        {
            //timeSinceLastMelee = 0;
            meleeWeapon.SetActive(true);
            meleeWeapon.GetComponent<Animator>().SetTrigger("swing");
            SoundEffectsManager.instance.PlayAudioClip(shovelSwingSound, true);
            yield return new WaitForSeconds(0.25f);
            meleeWeapon.SetActive(false);
        }

        IEnumerator RangedAttack()
        {
            yield return new WaitForSeconds(3f);
            while (distance > 10)
            {
                var dirToPlayer = (player.transform.position - transform.position).normalized;
                dirToPlayer.y = 0;
                transform.forward = dirToPlayer;
                state = EnemyStates.ranged;
                targetVelocity = Vector3.zero;
                currentStateElapsed = 0;
                Instantiate(rocket, rocketSpawn);
                yield return new WaitForSeconds(1.5f);
            }
        }

        void UpdateRanged()
        {
            if (distance < 10)
            {
                if(currentStateElapsed >= 1f)
                {
                    state = EnemyStates.idle;
                }
            }
        }

        void UpdateMelee()
        {
            //Debug.Log("in melee");
            if (currentStateElapsed >= 1.5f)
            {
                state = EnemyStates.idle;
            }
        }

        public void Death()
        {
            navigator.enabled = false;
            targetVelocity = Vector3.zero;
            GameManager.instance.GoToNextLevel();
            state = EnemyStates.dead;
        }

        void UpdateDead()
        {
            //Debug.Log("in dead");
        }

        bool AttemptMakePathToPlayer()
        {
            return (navigator.CalculatePathToPosition(player.position));
        }

        float DistanceToPlayer()
        {
            return Vector3.Distance(_transform.position, player.position);
        }

        public void SetInMeleeRange(bool inMeleeRange)
        {
            this.inMeleeRange = inMeleeRange;
        }

        IEnumerator SayRandomVoiceLines()
        {
            yield return new WaitForSeconds(10f);
            while(this.gameObject.activeSelf == true)
            {
                SoundEffectsManager.instance.PlayRandomClip(voiceLines.clips, true);
                yield return new WaitForSeconds(10f);
            }
        }
    }
}
