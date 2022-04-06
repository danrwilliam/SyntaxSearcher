# SyntaxSearch

A library for making it simple to use Roslyn to examine code bases.

## Example

```xml
<SyntaxSearchDefinition>
  <IfStatement>
    <NotEqualsExpression>
      <Anything Name="objectInvoked" />
      <NullLiteralExpression />
    </NotEqualsExpression>
    <Block>
      <ExpressionStatement>
        <InvocationExpression>
          <MatchCapture Name="objectInvoked" />
          <ArgumentList />
        </InvocationExpression>
      </ExpressionStatement>
    </Block>
  </IfStatement>
</SyntaxSearchDefinition>
```

This example search definition would match any of the following

```csharp
if (a != null)
{
    a();   
}

if (_member.Item != null)
{
    _member.Item(2 + 3);
}

// but it would not match
if (a !=  null)
{
    b();
}
```
