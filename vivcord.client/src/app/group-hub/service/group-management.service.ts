import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';
import { GroupChatDTO, CreateGroupChatDTO } from '../dto/group-hub.dto';

@Injectable({
  providedIn: 'root',
})
export class GroupManagementService {
  private readonly _http = inject(HttpClient);
  private readonly _apiUrl = `${environment.apiUrl}/Group`;

  public getMyGroups(): Observable<GroupChatDTO[]> {
    return this._http.get<GroupChatDTO[]>(
      `${this._apiUrl}/my-groups`,
      { withCredentials: true }
    );
  }

  public getGroup(groupId: number): Observable<GroupChatDTO> {
    return this._http.get<GroupChatDTO>(
      `${this._apiUrl}/get/${groupId}`,
      { withCredentials: true }
    );
  }

  public createGroup(name: string): Observable<GroupChatDTO> {
    const dto: CreateGroupChatDTO = { name };
    return this._http.post<GroupChatDTO>(
      `${this._apiUrl}/create`,
      dto,
      { withCredentials: true }
    );
  }

  public deleteGroup(groupId: number): Observable<void> {
    return this._http.delete<void>(
      `${this._apiUrl}/delete/${groupId}`,
      { withCredentials: true }
    );
  }

  public addMember(groupId: number, userName: string): Observable<void> {
    const params = new HttpParams().set('username', userName);
    return this._http.post<void>(
      `${this._apiUrl}/add-member/${groupId}`,
      {},
      { params, withCredentials: true }
    );
  }

  public removeMember(groupId: number, userName: string): Observable<void> {
    const params = new HttpParams().set('username', userName);
    return this._http.delete<void>(
      `${this._apiUrl}/remove-member/${groupId}`,
      { params, withCredentials: true }
    );
  }
}
