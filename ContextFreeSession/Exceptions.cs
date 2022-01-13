using System;
using System.Runtime.Serialization;

namespace ContextFreeSession
{
    namespace Design
    {
        [Serializable]
        public sealed class IdentifierConflictException : Exception
        {
            internal IdentifierConflictException() : base() { }

            internal IdentifierConflictException(string? message) : base(message) { }

            internal IdentifierConflictException(string? message, Exception? inner) : base(message, inner) { }

            private IdentifierConflictException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        }

        [Serializable]
        public class InvalidIdentifierException : Exception
        {
            internal InvalidIdentifierException() : base() { }

            internal InvalidIdentifierException(string? message) : base(message) { }

            internal InvalidIdentifierException(string? message, Exception? inner) : base(message, inner) { }

            protected InvalidIdentifierException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        }

        [Serializable]
        public sealed class InvalidNonterminalSymbolException : InvalidIdentifierException
        {
            internal InvalidNonterminalSymbolException() : base() { }

            internal InvalidNonterminalSymbolException(string nonterminal) : base($"'{nonterminal}' is not a valid nonterminal symbol.") { }

            internal InvalidNonterminalSymbolException(string? message, Exception? inner) : base(message, inner) { }

            private InvalidNonterminalSymbolException(SerializationInfo info, StreamingContext context) : base(info, context) { }

            public static bool IsValidNonterminalSymbol(string symbol)
            {
                return symbol.Length > 0 && symbol[0].IsLatin() && symbol.IsAlphanumeric();
            }
        }

        [Serializable]
        public sealed class InvalidRoleNameException : InvalidIdentifierException
        {
            internal InvalidRoleNameException() : base() { }

            internal InvalidRoleNameException(string roleName) : base($"'{roleName}' is not a valid role name.") { }

            internal InvalidRoleNameException(string? message, Exception? inner) : base(message, inner) { }

            private InvalidRoleNameException(SerializationInfo info, StreamingContext context) : base(info, context) { }

            public static bool IsValidRoleName(string roleName)
            {
                return roleName.Length > 0 && roleName[0].IsLatin() && roleName.IsAlphanumeric();
            }
        }

        [Serializable]
        public sealed class InvalidLabelException : InvalidIdentifierException
        {
            internal InvalidLabelException() : base() { }

            internal InvalidLabelException(string label) : base($"'{label}' is not a valid label.") { }

            internal InvalidLabelException(string? message, Exception? inner) : base(message, inner) { }

            private InvalidLabelException(SerializationInfo info, StreamingContext context) : base(info, context) { }

            public static bool IsValidLabel(string label)
            {
                return label.Length > 0 && label[0].IsLatin() && label.IsAlphanumeric();
            }
        }

        [Serializable]
        public class InvalidGlobalTypeException : Exception
        {
            internal InvalidGlobalTypeException() : base() { }

            internal InvalidGlobalTypeException(string? message) : base(message) { }

            internal InvalidGlobalTypeException(string? message, Exception? inner) : base(message, inner) { }

            protected InvalidGlobalTypeException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        }

        [Serializable]
        public sealed class ReflexiveMessageException : InvalidGlobalTypeException
        {
            internal ReflexiveMessageException() : base() { }

            internal ReflexiveMessageException(string roleName) : base($"Reflexive message from {roleName} to {roleName}.") { }

            internal ReflexiveMessageException(string? message, Exception? inner) : base(message, inner) { }

            private ReflexiveMessageException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        }

        [Serializable]
        public class ProjectionException : Exception
        {
            internal ProjectionException() : base() { }

            internal ProjectionException(string? message) : base(message) { }

            internal ProjectionException(string message, LocalType lastState) : base(message.WithNewLine() + "Last state:".WithNewLine() + lastState.ToString().Indented(2)) { }

            internal ProjectionException(string? message, Exception? inner) : base(message, inner) { }

            protected ProjectionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        }

        [Serializable]
        public sealed class LeftRecursionException : ProjectionException
        {
            internal LeftRecursionException() : base() { }

            internal LeftRecursionException(string? message) : base(message) { }

            internal LeftRecursionException(string message, LocalType lastState) : base(message.WithNewLine() + "Last state:".WithNewLine() + lastState.ToString().Indented(2)) { }

            internal LeftRecursionException(string? message, Exception? inner) : base(message, inner) { }

            private LeftRecursionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        }
    }

    namespace Runtime
    {
        [Serializable]
        public sealed class LinearityViolationException : InvalidOperationException
        {
            internal LinearityViolationException() : base() { }

            internal LinearityViolationException(string? message) : base(message) { }

            internal LinearityViolationException(string? message, Exception? inner) : base(message, inner) { }

            private LinearityViolationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        }

        [Serializable]
        public sealed class UnexpectedLabelException : Exception
        {
            internal UnexpectedLabelException() : base() { }

            internal UnexpectedLabelException(string? message) : base(message) { }

            internal UnexpectedLabelException(string? message, Exception? inner) : base(message, inner) { }

            private UnexpectedLabelException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        }
    }
}
