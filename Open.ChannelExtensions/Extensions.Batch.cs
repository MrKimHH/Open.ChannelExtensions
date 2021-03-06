﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading.Channels;

namespace Open.ChannelExtensions
{
	public static partial class Extensions
	{
		class BatchingChannelReader<T> : BufferingChannelReader<T, List<T>>
		{
			private readonly int _batchSize;
			private List<T>? _current;

			public BatchingChannelReader(ChannelReader<T> source, int batchSize, bool singleReader, bool syncCont = false) : base(source, singleReader, syncCont)
			{
				if (batchSize < 1) throw new ArgumentOutOfRangeException(nameof(batchSize), batchSize, "Must be at least 1.");
				Contract.EndContractBlock();

				_batchSize = batchSize;
				_current = source.Completion.IsCompleted ? null : new List<T>(batchSize);
			}

			protected override bool TryPipeItems()
			{
				if (_current == null || Buffer == null || Buffer.Reader.Completion.IsCompleted)
					return false;

				lock (Buffer)
				{
					var c = _current;
					if (c == null || Buffer.Reader.Completion.IsCompleted)
						return false;

					var source = Source;
					if (source == null || source.Completion.IsCompleted)
					{
						// All finished, release the last batch to the buffer.
						c.TrimExcess();
						_current = null;
						if (c.Count == 0)
							return false;

						Buffer.Writer.TryWrite(c);
						return true;
					}

					while (source.TryRead(out T item))
					{
						if (c.Count == _batchSize)
						{
							_current = new List<T>(_batchSize) { item };
							Buffer.Writer.TryWrite(c);
							return true;
						}

						c.Add(item);
					}

					return false;
				}
			}
		}

		/// <summary>
		/// Batches results into the batch size provided with a max capacity of batches.
		/// </summary>
		/// <typeparam name="T">The output type of the source channel.</typeparam>
		/// <param name="source">The channel to read from.</param>
		/// <param name="batchSize">The maximum size of each batch.</param>
		/// <param name="singleReader">True will cause the resultant reader to optimize for the assumption that no concurrent read operations will occur.</param>
		/// <param name="allowSynchronousContinuations">True can reduce the amount of scheduling and markedly improve performance, but may produce unexpected or even undesirable behavior.</param>
		/// <returns>A channel reader containing the batches.</returns>
		public static ChannelReader<List<T>> Batch<T>(this ChannelReader<T> source, int batchSize, bool singleReader = false, bool allowSynchronousContinuations = false)
			=> new BatchingChannelReader<T>(source ?? throw new ArgumentNullException(nameof(source)), batchSize, singleReader, allowSynchronousContinuations);
	}
}
