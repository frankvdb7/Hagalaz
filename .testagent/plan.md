# Test plan

1. Add a MassTransit harness consumer that faults the first two update requests and responds on the third.
2. Exercise `CharacterPersistenceService.PersistAsync` with a dehydrated character model.
3. Assert three attempts and a successful completion; assert the generated request metadata.
