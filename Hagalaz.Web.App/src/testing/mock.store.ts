import { NgModule, Injectable, inject } from "@angular/core";
import {
  Store,
  StateObservable,
  ActionsSubject,
  ReducerManager,
  StoreModule
} from "@ngrx/store";
import { BehaviorSubject } from "rxjs";
import { NoopAnimationsModule } from "@angular/platform-browser/animations";
import { RouterTestingModule } from "@angular/router/testing";

@Injectable()
export class MockStore<T> extends Store<T> {
  private stateSubject = new BehaviorSubject<T>({} as T);

  /** Inserted by Angular inject() migration for backwards compatibility */
  constructor(...args: unknown[]);

  constructor() {
    const state$ = inject(StateObservable);
    const actionsObserver = inject(ActionsSubject);
    const reducerManager = inject(ReducerManager);

    super(state$, actionsObserver, reducerManager);
    this.source = this.stateSubject.asObservable();
  }

  setState(nextState: T) {
    this.stateSubject.next(nextState);
  }
}

export function provideMockStore() {
  return {
    provide: Store,
    useClass: MockStore
  };
}

@NgModule({
  imports: [NoopAnimationsModule, RouterTestingModule, StoreModule.forRoot({})],
  exports: [NoopAnimationsModule, RouterTestingModule],
  providers: [provideMockStore()]
})
export class TestingModule {
  constructor() {}
}
