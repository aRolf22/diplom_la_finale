# Atan

Menu Path : **Operator > Math > Trigonometry > Atan**

The **Atan** Operator calculates the [arctangent](https://docs.unity3d.com/ScriptReference/Mathf.Atan.html) of the input. This Operator is the inverse of the [Tangent Operator](Operator-Tangent.md). The result is in radians.

This Operator accepts input values of various types. For the list of types this Operator can use, see [Available Types](#available-types).

## Operator properties

| **Property** | **Type**                                | **Description**                                              |
| ------------ | --------------------------------------- | ------------------------------------------------------------ |
| **X**        | [Configurable](#operator-configuration) | An input of the  [Available Types](#available-types)          |
| **Out**      | Dependent                               | The arctan of the input.<br/>The **Type** changes to match the type of **X**. |

## Operator configuration

To view the Operator's configuration, click the **cog** icon in the Operator's header. Use the drop-down to select the type for the **X** port. For the list of types this property supports, see [Available types](#available-types).



### Available types

You can use the following types for your **input** ports:

- **float**
- **Vector**
- **Vector2**
- **Vector3**
- **Vector4**
- **Position**
- **Direction**
