﻿namespace Fiero.Business
{
    public readonly struct MoveRandomlyAction : IAction
    {
        ActionName IAction.Name => ActionName.Move;
    }
}
