using Godot;

public partial class Camera : Camera2D
{
	private const float CameraSpeed = 500.0f;
	
    private const float ZoomStep = 0.05f;
    private const float ZoomSpeed = 10.0f;
    private const float MinZoom = 0.5f;
    private const float MaxZoom = 2.0f;

	private Vector2 _targetZoom = Vector2.One;

	public override void _Process(double delta)
	{
		Vector2 direction = Vector2.Zero;

		if (Input.IsKeyPressed(Key.W))
			direction.Y -= 1;

		if (Input.IsKeyPressed(Key.S))
			direction.Y += 1;

		if (Input.IsKeyPressed(Key.A))
			direction.X -= 1;

		if (Input.IsKeyPressed(Key.D))
			direction.X += 1;
		
		if (direction != Vector2.Zero)
		{
			direction = direction.Normalized();

			Position += direction * CameraSpeed * (float)delta;
		}

		Zoom = Zoom.Lerp(_targetZoom, 1.0f - Mathf.Exp(-ZoomSpeed * (float)delta));
	}

    public override void _Input(InputEvent @event)
    {
     	if (@event is not InputEventMouseButton mouseEvent)
			return;

		if (!mouseEvent.Pressed)
			return;


        if (mouseEvent.ButtonIndex == MouseButton.WheelUp)
        {
            _targetZoom += Vector2.One * ZoomStep;
        }
        else if (mouseEvent.ButtonIndex == MouseButton.WheelDown)
        {
            _targetZoom -= Vector2.One * ZoomStep;
        }

		_targetZoom.X = Mathf.Clamp(_targetZoom.X, MinZoom, MaxZoom);
		_targetZoom.Y = Mathf.Clamp(_targetZoom.Y, MinZoom, MaxZoom);
    }
}
