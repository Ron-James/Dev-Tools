using System.Threading.Tasks;
using UnityEngine;

public interface ICommand<in T> : ICommand
{
    Task Execute(T parameter);
}
