﻿using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Open.ChannelExtensions
{
	public static partial class Extensions
	{
        private static Channel<T> CreateChannel<T>(int capacity = -1, bool singleReader = false)
            => capacity > 0
                ? Channel.CreateBounded<T>(new BoundedChannelOptions(capacity)
                {
                    SingleWriter = true,
                    SingleReader = singleReader,
                    AllowSynchronousContinuations = true,
                    FullMode = BoundedChannelFullMode.Wait
                })
                : Channel.CreateUnbounded<T>(new UnboundedChannelOptions
                {
                    SingleWriter = true,
                    SingleReader = singleReader,
                    AllowSynchronousContinuations = true
                });

        /// <summary>
        /// Reads all entries concurrently and applies the values to the provided transform function before buffering the results into another channel for consumption.
        /// </summary>
        /// <typeparam name="TIn">The type contained by the source channel.</typeparam>
        /// <typeparam name="TOut">The outgoing type from the resultant channel.</typeparam>
        /// <param name="source">The source channel.</param>
        /// <param name="maxConcurrency">The maximum number of concurrent operations.</param>
        /// <param name="transform">The transform function to apply the source entries before passing on to the output.</param>
        /// <param name="capacity">The width of the pipe: how many entries to buffer while waiting to be read from.</param>
        /// <param name="singleReader">True will cause the resultant reader to optimize for the assumption that no concurrent read operations will occur.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The channel reader containing the output.</returns>
        public static ChannelReader<TOut> PipeAsync<TIn, TOut>(this ChannelReader<TIn> source,
            int maxConcurrency, Func<TIn, ValueTask<TOut>> transform, int capacity = -1, bool singleReader = false,
            CancellationToken cancellationToken = default)
        {
            var channel = CreateChannel<TOut>(capacity, singleReader);
            var writer = channel.Writer;

            source
                .ReadAllConcurrentlyAsync(maxConcurrency, e =>
                {
                    if (cancellationToken.IsCancellationRequested)
                        return new ValueTask(Task.FromCanceled(cancellationToken));
                    var result = transform(e);
                    return result.IsCompletedSuccessfully
                        ? writer.WriteAsync(result.Result, cancellationToken)
                        : ValueNotReady(result); // Result is not ready, so we need to wait for it.
                }, cancellationToken)
                .ContinueWith(t =>
                {
                    writer.Complete(t.Exception);
                });

            return channel.Reader;

            async ValueTask ValueNotReady(ValueTask<TOut> value)
            {
                var r = await value;
                await writer.WriteAsync(r, cancellationToken);
            }
        }

        /// <summary>
        /// Reads all entries and applies the values to the provided transform function before buffering the results into another channel for consumption.
        /// </summary>
        /// <typeparam name="TIn">The type contained by the source channel.</typeparam>
        /// <typeparam name="TOut">The outgoing type from the resultant channel.</typeparam>
        /// <param name="source">The source channel.</param>
        /// <param name="maxConcurrency">The maximum number of concurrent operations.</param>
        /// <param name="transform">The transform function to apply the source entries before passing on to the output.</param>
        /// <param name="capacity">The width of the pipe: how many entries to buffer while waiting to be read from.</param>
        /// <param name="singleReader">True will cause the resultant reader to optimize for the assumption that no concurrent read operations will occur.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The channel reader containing the output.</returns>
        public static ChannelReader<TOut> PipeAsync<TIn, TOut>(this Channel<TIn> source,
            int maxConcurrency, Func<TIn, ValueTask<TOut>> transform, int capacity = -1, bool singleReader = false,
            CancellationToken cancellationToken = default)
            => source.Reader.PipeAsync(maxConcurrency, transform, capacity, singleReader, cancellationToken);

        /// <summary>
        /// Reads all entries and applies the values to the provided transform function before buffering the results into another channel for consumption.
        /// </summary>
        /// <typeparam name="TIn">The type contained by the source channel.</typeparam>
        /// <typeparam name="TOut">The outgoing type from the resultant channel.</typeparam>
        /// <param name="source">The source channel.</param>
        /// <param name="maxConcurrency">The maximum number of concurrent operations.</param>
        /// <param name="transform">The transform function to apply the source entries before passing on to the output.</param>
        /// <param name="capacity">The width of the pipe: how many entries to buffer while waiting to be read from.</param>
        /// <param name="singleReader">True will cause the resultant reader to optimize for the assumption that no concurrent read operations will occur.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The channel reader containing the output.</returns>
        public static ChannelReader<TOut> PipeAsync<TIn, TOut>(this ChannelReader<TIn> source,
            int maxConcurrency, Func<TIn, Task<TOut>> transform, int capacity = -1, bool singleReader = false,
            CancellationToken cancellationToken = default)
            => source.PipeAsync(maxConcurrency, e => new ValueTask<TOut>(transform(e)), capacity, singleReader, cancellationToken);

        /// <summary>
        /// Reads all entries and applies the values to the provided transform function before buffering the results into another channel for consumption.
        /// </summary>
        /// <typeparam name="TIn">The type contained by the source channel.</typeparam>
        /// <typeparam name="TOut">The outgoing type from the resultant channel.</typeparam>
        /// <param name="source">The source channel.</param>
        /// <param name="maxConcurrency">The maximum number of concurrent operations.</param>
        /// <param name="transform">The transform function to apply the source entries before passing on to the output.</param>
        /// <param name="capacity">The width of the pipe: how many entries to buffer while waiting to be read from.</param>
        /// <param name="singleReader">True will cause the resultant reader to optimize for the assumption that no concurrent read operations will occur.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The channel reader containing the output.</returns>
        public static ChannelReader<TOut> PipeAsync<TIn, TOut>(this Channel<TIn> source,
            int maxConcurrency, Func<TIn, Task<TOut>> transform, int capacity = -1, bool singleReader = false,
            CancellationToken cancellationToken = default)
            => source.Reader.PipeAsync(maxConcurrency, transform, capacity, singleReader, cancellationToken);

        /// <summary>
        /// Reads all entries and applies the values to the provided transform function before buffering the results into another channel for consumption.
        /// </summary>
        /// <typeparam name="TIn">The type contained by the source channel.</typeparam>
        /// <typeparam name="TOut">The outgoing type from the resultant channel.</typeparam>
        /// <param name="source">The source channel.</param>
        /// <param name="maxConcurrency">The maximum number of concurrent operations.</param>
        /// <param name="transform">The transform function to apply the source entries before passing on to the output.</param>
        /// <param name="capacity">The width of the pipe: how many entries to buffer while waiting to be read from.</param>
        /// <param name="singleReader">True will cause the resultant reader to optimize for the assumption that no concurrent read operations will occur.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The channel reader containing the output.</returns>
        public static ChannelReader<TOut> Pipe<TIn, TOut>(this ChannelReader<TIn> source,
            int maxConcurrency, Func<TIn, TOut> transform, int capacity = -1, bool singleReader = false,
            CancellationToken cancellationToken = default)
            => source.PipeAsync(maxConcurrency, e => new ValueTask<TOut>(transform(e)), capacity, singleReader, cancellationToken);

        /// <summary>
        /// Reads all entries and applies the values to the provided transform function before buffering the results into another channel for consumption.
        /// </summary>
        /// <typeparam name="TIn">The type contained by the source channel.</typeparam>
        /// <typeparam name="TOut">The outgoing type from the resultant channel.</typeparam>
        /// <param name="source">The source channel.</param>
        /// <param name="maxConcurrency">The maximum number of concurrent operations.</param>
        /// <param name="transform">The transform function to apply the source entries before passing on to the output.</param>
        /// <param name="capacity">The width of the pipe: how many entries to buffer while waiting to be read from.</param>
        /// <param name="singleReader">True will cause the resultant reader to optimize for the assumption that no concurrent read operations will occur.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The channel reader containing the output.</returns>
        public static ChannelReader<TOut> Pipe<TIn, TOut>(this Channel<TIn> source,
            int maxConcurrency, Func<TIn, TOut> transform, int capacity = -1, bool singleReader = false,
            CancellationToken cancellationToken = default)
            => source.Reader.Pipe(maxConcurrency, transform, capacity, singleReader, cancellationToken);

        /// <summary>
        /// Reads all entries and applies the values to the provided transform function before buffering the results into another channel for consumption.
        /// </summary>
        /// <typeparam name="TIn">The type contained by the source channel.</typeparam>
        /// <typeparam name="TOut">The outgoing type from the resultant channel.</typeparam>
        /// <param name="source">The source channel.</param>
        /// <param name="transform">The transform function to apply the source entries before passing on to the output.</param>
        /// <param name="capacity">The width of the pipe: how many entries to buffer while waiting to be read from.</param>
        /// <param name="singleReader">True will cause the resultant reader to optimize for the assumption that no concurrent read operations will occur.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The channel reader containing the output.</returns>
        public static ChannelReader<TOut> PipeAsync<TIn, TOut>(this ChannelReader<TIn> source,
            Func<TIn, ValueTask<TOut>> transform, int capacity = -1, bool singleReader = false,
            CancellationToken cancellationToken = default)
            => PipeAsync(source, 1, transform, capacity, singleReader, cancellationToken);

        /// <summary>
        /// Reads all entries and applies the values to the provided transform function before buffering the results into another channel for consumption.
        /// </summary>
        /// <typeparam name="TIn">The type contained by the source channel.</typeparam>
        /// <typeparam name="TOut">The outgoing type from the resultant channel.</typeparam>
        /// <param name="source">The source channel.</param>
        /// <param name="transform">The transform function to apply the source entries before passing on to the output.</param>
        /// <param name="capacity">The width of the pipe: how many entries to buffer while waiting to be read from.</param>
        /// <param name="singleReader">True will cause the resultant reader to optimize for the assumption that no concurrent read operations will occur.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The channel reader containing the output.</returns>
        public static ChannelReader<TOut> PipeAsync<TIn, TOut>(this Channel<TIn> source,
			Func<TIn, ValueTask<TOut>> transform, int capacity = -1, bool singleReader = false,
			CancellationToken cancellationToken = default)
			=> source.Reader.PipeAsync(transform, capacity, singleReader, cancellationToken);

        /// <summary>
        /// Reads all entries and applies the values to the provided transform function before buffering the results into another channel for consumption.
        /// </summary>
        /// <typeparam name="TIn">The type contained by the source channel.</typeparam>
        /// <typeparam name="TOut">The outgoing type from the resultant channel.</typeparam>
        /// <param name="source">The source channel.</param>
        /// <param name="transform">The transform function to apply the source entries before passing on to the output.</param>
        /// <param name="capacity">The width of the pipe: how many entries to buffer while waiting to be read from.</param>
        /// <param name="singleReader">True will cause the resultant reader to optimize for the assumption that no concurrent read operations will occur.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The channel reader containing the output.</returns>
        public static ChannelReader<TOut> PipeAsync<TIn, TOut>(this ChannelReader<TIn> source,
			Func<TIn, Task<TOut>> transform, int capacity = -1, bool singleReader = false,
            CancellationToken cancellationToken = default)
			=> source.PipeAsync(e => new ValueTask<TOut>(transform(e)), capacity, singleReader, cancellationToken);

        /// <summary>
        /// Reads all entries and applies the values to the provided transform function before buffering the results into another channel for consumption.
        /// </summary>
        /// <typeparam name="TIn">The type contained by the source channel.</typeparam>
        /// <typeparam name="TOut">The outgoing type from the resultant channel.</typeparam>
        /// <param name="source">The source channel.</param>
        /// <param name="transform">The transform function to apply the source entries before passing on to the output.</param>
        /// <param name="capacity">The width of the pipe: how many entries to buffer while waiting to be read from.</param>
        /// <param name="singleReader">True will cause the resultant reader to optimize for the assumption that no concurrent read operations will occur.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The channel reader containing the output.</returns>
        public static ChannelReader<TOut> PipeAsync<TIn, TOut>(this Channel<TIn> source,
			Func<TIn, Task<TOut>> transform, int capacity = -1, bool singleReader = false,
            CancellationToken cancellationToken = default)
			=> source.Reader.PipeAsync(transform, capacity, singleReader, cancellationToken);

        /// <summary>
        /// Reads all entries and applies the values to the provided transform function before buffering the results into another channel for consumption.
        /// </summary>
        /// <typeparam name="TIn">The type contained by the source channel.</typeparam>
        /// <typeparam name="TOut">The outgoing type from the resultant channel.</typeparam>
        /// <param name="source">The source channel.</param>
        /// <param name="transform">The transform function to apply the source entries before passing on to the output.</param>
        /// <param name="capacity">The width of the pipe: how many entries to buffer while waiting to be read from.</param>
        /// <param name="singleReader">True will cause the resultant reader to optimize for the assumption that no concurrent read operations will occur.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The channel reader containing the output.</returns>
        public static ChannelReader<TOut> Pipe<TIn, TOut>(this ChannelReader<TIn> source,
			Func<TIn, TOut> transform, int capacity = -1, bool singleReader = false,
            CancellationToken cancellationToken = default)
			=> source.PipeAsync(e => new ValueTask<TOut>(transform(e)), capacity, singleReader, cancellationToken);

        /// <summary>
        /// Reads all entries and applies the values to the provided transform function before buffering the results into another channel for consumption.
        /// </summary>
        /// <typeparam name="TIn">The type contained by the source channel.</typeparam>
        /// <typeparam name="TOut">The outgoing type from the resultant channel.</typeparam>
        /// <param name="source">The source channel.</param>
        /// <param name="transform">The transform function to apply the source entries before passing on to the output.</param>
        /// <param name="capacity">The width of the pipe: how many entries to buffer while waiting to be read from.</param>
        /// <param name="singleReader">True will cause the resultant reader to optimize for the assumption that no concurrent read operations will occur.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The channel reader containing the output.</returns>
        public static ChannelReader<TOut> Pipe<TIn, TOut>(this Channel<TIn> source,
			Func<TIn, TOut> transform, int capacity = -1, bool singleReader = false,
            CancellationToken cancellationToken = default)
			=> source.Reader.Pipe(transform, capacity, singleReader, cancellationToken);
	}
}