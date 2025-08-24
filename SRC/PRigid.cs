using Godot;
using System;

public partial class PRigid : RigidBody2D
{
	[Export]
	public float EXP_VerticalV = 2000f;
	[Export]
	public float EXP_HorizonV = 1000f;
	[Export]
	public AnimatedSprite2D EXP_anim;

	// public override void _Input(InputEvent @event)
	// {
	// 	// Mouse in viewport coordinates.
	// 	if (@event is InputEventMouseButton eventMouseButton)
	// 	{
	// 		GD.Print("Mouse Click/Unclick at: ", eventMouseButton.Position);
	// 	}
	// 	else if (@event is InputEventMouseMotion eventMouseMotion)
	// 	{
	// 		GD.Print("Mouse Motion at: ", eventMouseMotion.Position);
	// 	}

	// 	// Print the size of the viewport.
	// 	GD.Print("Viewport Resolution is: ", GetViewport().GetVisibleRect().Size);
	// }

	public override void _IntegrateForces(PhysicsDirectBodyState2D state)
	{
		base._IntegrateForces(state);
		var v = state.LinearVelocity;
		var step = state.Step;
		var inputAxis = Vector2.Zero;

		if (Input.IsActionPressed("MovD"))
		{
			inputAxis.X += EXP_HorizonV;
			EXP_anim.FlipH = true;
		}

		if (Input.IsActionPressed("MovA"))
		{
			inputAxis.X -= EXP_HorizonV;
			EXP_anim.FlipH = false;
		}

		if (Input.IsActionPressed("Jmp"))
		{
			inputAxis.Y -= EXP_VerticalV;
		}

		int floor_idx = -1;
		for (int i = 0; i < state.GetContactCount(); ++i)
		{
			var cNorm = state.GetContactLocalNormal(i);
			if (cNorm.Dot(new Vector2(0, -1)) > .6f)
			{
				floor_idx = i;
			}
		}

		if (inputAxis.LengthSquared() > 0)
		{
			var nv = inputAxis;

		}


	}
	public override void _Ready()
	{
		base._Ready();
		EXP_anim.Play();
	}
	public override void _Process(double delta)
	{
		base._Process(delta);
		// Rotation += 1f * (float)delta;


		// if (Input.IsActionPressed("MovW"))
		//     v.Y -= 1;

		// if (Input.IsActionPressed("MovS"))
		//     v.Y += 1;


		// Position = new Vector2(
		//     Mathf.Clamp(Position.X + v.X * EXP_MoveSpeed * (float)delta, 0, GetViewportRect().Size.X),
		//     Mathf.Clamp(Position.Y + v.Y * EXP_MoveSpeed * (float)delta, 0, GetViewportRect().Size.Y)
		// );
	}

}
