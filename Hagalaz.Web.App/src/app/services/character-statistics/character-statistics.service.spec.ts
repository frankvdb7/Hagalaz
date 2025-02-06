import { TestBed } from '@angular/core/testing';

import { CharacterStatisticsService } from './character-statistics.service';

describe('CharacterStatisticsService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: CharacterStatisticsService = TestBed.get(CharacterStatisticsService);
    expect(service).toBeTruthy();
  });
});
