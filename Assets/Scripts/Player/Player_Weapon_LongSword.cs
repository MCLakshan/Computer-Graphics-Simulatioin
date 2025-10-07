using StarterAssets;
using UnityEngine;

public class Player_Weapon_LongSword : MonoBehaviour
{
   [SerializeField] private FirstPersonController FpsController;
   
   private Animator animator;
   private float currentSpeed;
   private float maximumSpeed;
   
   private void Start()
   {
      animator = GetComponent<Animator>();
      maximumSpeed = FpsController.SprintSpeed;
   }

   private void Update()
   {
      currentSpeed = FpsController.GetSpeed();
      
      MovementAnimation();
   }
   
   private void MovementAnimation()
   {
      float velocity = Mathf.Clamp01(currentSpeed/maximumSpeed);
      
      animator.SetFloat("Velocity", velocity);
   }
   
   
   
}
