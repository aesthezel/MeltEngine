namespace MeltEngine.Entities;

public class Entity
{
    // Id es asignado por ECSOperator. 'internal set' permite que solo clases dentro del mismo ensamblado (MeltEngine) lo establezcan.
    public uint Id { get; internal set; }

    // Podríamos añadir un campo opcional para el nombre, si se decide que las entidades deben llevar su nombre.
    // public string Name { get; internal set; }
    // Esto ayudaría en debugging o si SceneSerializer asigna directamente el nombre a la entidad.
    // Por ahora, el nombre del JSON se usa principalmente para el mapeo en SceneSerializer.

    // Sobrescribir ToString() puede ser útil para debugging.
    public override string ToString()
    {
        return $"Entity(Id: {Id})"; // Añadir Name si se implementa: $"Entity(Id: {Id}, Name: {Name ?? "Unnamed"})";
    }

    // Para que las entidades puedan usarse como claves en diccionarios de forma eficiente (aunque ECSOperator usa el Id).
    public override bool Equals(object obj)
    {
        if (obj is Entity other)
        {
            return Id == other.Id;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}