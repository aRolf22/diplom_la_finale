# Set Position (Shape : Cone)

Menu Path : **Position > Set Position (Shape : Cone)**

The **Set Position (Shape : Cone)** Block calculatesa position based on an input [ArcCone ](Type-ArcCone .md) and stores the result in the [position attribute](Reference-Attributes.md), based on composition.

The ArcCone shape adds an arc property to the cone to determine its arc angle, in radians. setting an arc value of pi creates a half-cone.

This Block can calculate the position either from the ArcCone 's **Surface**, **Volume**, or **Thick Surface** where thickness can be relative to the size of the shape, or an absolute value.


This Block also calculates a direction vector based on the calculated position on the shape, and stores it to the [direction attribute](Reference-Attributes.md), based on composition. This direction is equal to the normalized vector from the center of the cone to the calculated position.

Note: [Velocity from Direction and Speed](Block-VelocityFromDirectionAndSpeed.md) Blocks can then process the direction attribute.

![](Images/Block-SetPosition(Cone)Main.gif)

## Block compatibility

This Block is compatible with the following Contexts:

- [Initialize](Context-Initialize.md)
- [Update](Context-Update.md)
- Any output Context

## Block settings

| **Setting**               | **Type** | **Description**                                              |
| ------------------------- | -------- | ------------------------------------------------------------ |
| **Height Mode**           | Enum     | Specifies how this Block handles the ArcCone's volume. The options are:<br/>&#8226; **Volume**: Uses the entire cone to calculate a position.<br/>&#8226; **Base**: Only uses the circle at the base of the cone to calculate a position. |
| **Position Mode**         | Enum     | Specifies how this Block uses the shape to calculate a position. The options are:<br/>&#8226; **Surface**: Calculates positions only on the shape’s surface.<br/>&#8226; **Volume**: Calculates positions inside the entire shape’s volume.<br/>&#8226; **Thickness Absolute**: Calculates positions on a thick surface of given absolute thickness.<br/>&#8226; **Thickness Relative** will compute a position on a thick surface of a given percentage of the Shape’s size.of a given percentage of the Shape’s size. |
| **Spawn Mode**            | Enum     | The method this Block uses to distribute particles along the shape’s arc. <br/>&#8226; **Random**: Calculates a per-particle random progress (0..1) on the arc. <br/>&#8226; **Custom**: Allows you to specify the progress in the **Arc Sequencer** property. |
| **Composition Position**  | Enum     | **(Inspector)** Specifies how this Block composes the position attribute. The options are:<br/>&#8226; **Set**: Overwrites the position attribute with the new value.<br/>&#8226; **Add**: Adds the new value to the position attribute value.<br/>&#8226; **Multiply**: Multiplies the position attribute value by the new value.<br/>&#8226; **Blend**: Interpolates between the position attribute value and the new value. You can specify the blend factor between 0 and 1. |
| **Composition Direction** | Enum     | **(Inspector)** Specifies how this Block composes the direction attribute. The options are:<br/>&#8226; **Set**: Overwrites the position attribute with the new value.<br/>&#8226; **Add**: Adds the new value to the position attribute value.<br/>&#8226; **Multiply**: Multiplies the position attribute value by the new value.<br/>&#8226; **Blend**: Interpolates between the position attribute value and the new value. You can specify the blend factor between 0 and 1. |

## Block properties

| **Input**           | **Type**                   | **Description**                                              |
| ------------------- | -------------------------- | ------------------------------------------------------------ |
| **ArcCone**         | [ArcCone](Type-ArcCone.md) | The ArcCone that determines the shape to calculate the position from. |
| **Thickness**       | Float                      | The thickness of the shape’s surface for position calculation.<br/>This property only appears if you set **Position Mode** to **Thickness Relative** or **Thickness Absolute**. |
| **Arc Sequencer**   | Float                      | The position in the arc to spawn particles.<br/>This property only appears if you set **Spawn Mode** to **Custom**. |
| **Blend Position**  | Float                      | The blend percentage between the current position attribute value and the newly calculated position value.<br/>This property only appears if you set **Composition Position** to **Blend**. |
| **Blend Direction** | Float                      | The blend percentage between the current direction attribute value and the newly calculated direction value.<br/>This property only appears if you set **Composition Direction** to **Blend**. |
