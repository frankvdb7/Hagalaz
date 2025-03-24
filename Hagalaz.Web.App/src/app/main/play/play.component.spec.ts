import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { PlayComponent } from './play.component';

describe('PlayComponent', () => {
  let component: PlayComponent;
  let fixture: ComponentFixture<PlayComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
    imports: [PlayComponent]
})
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PlayComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
