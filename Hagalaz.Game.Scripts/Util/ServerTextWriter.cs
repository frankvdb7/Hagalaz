using System;
using System.IO;
using System.Text;

namespace Hagalaz.Game.Scripts.Util
{
    /// <summary>
    /// </summary>
    internal class ServerTextWriter : TextWriter
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ServerTextWriter" /> class.
        /// </summary>
        /// <param name="stringWriteCallBack">The string write call back.</param>
        public ServerTextWriter(Action<string> stringWriteCallBack) => _stringWriteCallBack = stringWriteCallBack;

        /// <summary>
        ///     The string write call back.
        /// </summary>
        private readonly Action<string> _stringWriteCallBack;

        /// <summary>
        ///     The builder.
        /// </summary>
        private readonly StringBuilder _builder = new StringBuilder();

        /// <summary>
        ///     Writes a string to the text string or stream.
        /// </summary>
        /// <param name="value">The string to write.</param>
        public override void Write(string value)
        {
            base.Write(value);
            _builder.Append(value);
        }

        /// <summary>
        ///     Writes a string followed by a line terminator to the text string or stream.
        /// </summary>
        /// <param name="value">The string to write. If <paramref name="value" /> is null, only the line terminator is written.</param>
        public override void WriteLine(string value)
        {
            base.WriteLine(value);
            _builder.Append(value);
            _stringWriteCallBack.Invoke(_builder.ToString());
            _builder.Clear();
        }

        /// <summary>
        ///     When overridden in a derived class, returns the character encoding in which the output is written.
        /// </summary>
        /// <returns>The character encoding in which the output is written.</returns>
        public override Encoding Encoding => Encoding.UTF8;
    }
}