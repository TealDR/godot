using Godot;
using System;

public partial class PlayerMovement : CharacterBody2D
{
	public const float JumpMomentumGain = -400.0f;
	public float Gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
	
	public const float MovementMomentumGain = 75.0f;
	public const float DashMomentumGain = 400.0f;
	
	public const float DashCooldown = 2.0f;
	public const float JumpBufferDuration = 0.15f;
	
	public float LastDashTimestamp = -10f;
	public float LastJumpTimestamp = -10f;
	public float AccumulatedDeltaTime = 0f;
	
	public bool CanDash = true;
	
	public Vector2 CurrMovementMomentum = new Vector2(0, 0);
	public Vector2 CurrDashMomentum = new Vector2(0, 0);
	
	public override void _PhysicsProcess(double Delta)
	{
		Vector2 NewVelocity = Velocity;
		
		AccumulatedDeltaTime += (float)Delta;
		if (AccumulatedDeltaTime > LastDashTimestamp + DashCooldown) {
			LastDashTimestamp = AccumulatedDeltaTime;
			CanDash = true;
		}
		
		if (AccumulatedDeltaTime > LastJumpTimestamp && AccumulatedDeltaTime < LastJumpTimestamp + JumpBufferDuration && IsOnFloor()) {
			NewVelocity.Y = JumpMomentumGain + CurrDashMomentum.Y;
			LastJumpTimestamp = 0;
		}
		
		if (IsOnFloor()) {
			CurrMovementMomentum *= 0.975f;
			CurrDashMomentum *= 0.6f;
		} else {
			CurrMovementMomentum *= 0.98f;
			CurrDashMomentum *= 0.98f;
		}
		
		if (!IsOnFloor())
			NewVelocity.Y += Gravity * (float)Delta;
		
		if (Input.IsActionJustPressed("Jump")) {
			Jump(ref NewVelocity);
		}
		
		Vector2 WASDDirection = new Vector2(
			Input.GetAxis("Left", "Right"), 
			Input.GetAxis("Up", "Down")
		);
		Vector2 ArrowKeysDirection = new Vector2(
			Input.GetAxis("ArrowLeft", "ArrowRight"), 
			Input.GetAxis("ArrowUp", "ArrowDown")
		);
		
		if (WASDDirection != Vector2.Zero) {
			CurrMovementMomentum.X += WASDDirection.X * MovementMomentumGain * (float)(Delta / 0.1);
		} else {
			NewVelocity.X = Mathf.MoveToward(Velocity.X, 0, CurrMovementMomentum.X + CurrDashMomentum.X);
		}
		
		if (Input.IsActionJustPressed("Dash") && CanDash) {
			Dash(WASDDirection, ArrowKeysDirection, ref NewVelocity);
		}
		
		NewVelocity.X = CurrMovementMomentum.X + CurrDashMomentum.X;
		Velocity = NewVelocity;
		MoveAndSlide();
	}
	
	public void Dash(Vector2 WASDDirection, Vector2 ArrowKeysDirection, ref Vector2 NewVelocity)
	{
		CanDash = false;
		LastDashTimestamp = AccumulatedDeltaTime;
		Vector2 DashDir;
		if (ArrowKeysDirection == Vector2.Zero) {
			DashDir = WASDDirection * DashMomentumGain;
		} else {
			DashDir = ArrowKeysDirection * DashMomentumGain;
		}
		Vector2 VelocityToApply = new Vector2(DashDir.X * 0.6f, DashDir.Y * 0.8f);
		bool OppositeMovement = Math.Sign(VelocityToApply.X) == -Math.Sign(CurrMovementMomentum.X);
		if (OppositeMovement) {
			float Multiplier = -CurrMovementMomentum.X / VelocityToApply.X;
			float NewXVelocity = VelocityToApply.X + VelocityToApply.X * Multiplier;
			VelocityToApply = new Vector2(NewXVelocity, VelocityToApply.Y);
		}
		CurrDashMomentum += VelocityToApply;
		NewVelocity.Y += CurrDashMomentum.Y;
		CurrMovementMomentum += VelocityToApply;
	}
	
	public void Jump(ref Vector2 NewVelocity)
	{
		if (IsOnFloor()) {
			NewVelocity.Y = JumpMomentumGain + CurrDashMomentum.Y;
		} else if (IsOnWall()) {
			WallJump(ref NewVelocity);
		} else {
			LastJumpTimestamp = AccumulatedDeltaTime;
		}
	}
	
	
	public void WallJump(ref Vector2 NewVelocity)
	{
		Godot.Collections.Dictionary RayResult = RaycastToWall();
		if (RayResult == null)
			return;
		
		if (RayResult.Count > 0) {
			CurrMovementMomentum.X = -CurrMovementMomentum.X + 100f * ((Vector2)RayResult["normal"]).X;
			CurrDashMomentum.X = -CurrDashMomentum.X;
			NewVelocity.Y += -400f;
		}
	}
	
	public Godot.Collections.Dictionary RaycastToWall()
	{
		var SpaceState = GetWorld2D().DirectSpaceState;
		int RayRange = 33;
		Vector2 LeftTargetPosition = GlobalPosition - new Vector2(RayRange, 0);
		Vector2 RightTargetPosition = GlobalPosition + new Vector2(RayRange, 0);
		
		var LeftQuery = PhysicsRayQueryParameters2D.Create(GlobalPosition, LeftTargetPosition);
		var RightQuery = PhysicsRayQueryParameters2D.Create(GlobalPosition, RightTargetPosition);
		
		Godot.Collections.Dictionary LeftResult = SpaceState.IntersectRay(LeftQuery);
		Godot.Collections.Dictionary RightResult = SpaceState.IntersectRay(RightQuery);
		
		if (LeftResult.Count == 0 && RightResult.Count == 0)
			return null;
		
		Godot.Collections.Dictionary ClosestRay;
		if (LeftResult.Count > 0 && RightResult.Count > 0) {
			float DistanceToLeft = Math.Abs(GlobalPosition.X - ((Vector2)LeftResult["position"]).X);
			float DistanceToRight = Math.Abs(GlobalPosition.X - ((Vector2)RightResult["position"]).X);
			if (DistanceToLeft >= DistanceToRight) {
				ClosestRay = RightResult;
			} else {
				ClosestRay = LeftResult;
			}
		} else if (LeftResult.Count > 0) {
			ClosestRay = LeftResult;
		} else {
			ClosestRay = RightResult;
		}
		return ClosestRay;
	}
}
