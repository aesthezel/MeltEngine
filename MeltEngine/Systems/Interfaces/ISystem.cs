using MeltEngine.Core;

namespace MeltEngine.Systems.Interfaces;

public interface ISystem
{
    void Update(ECSOperator entityOperator, float deltaTime);
}