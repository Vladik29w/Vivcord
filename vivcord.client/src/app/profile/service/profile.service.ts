import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, firstValueFrom } from 'rxjs';
import { environment } from '@environments/environment';
import {
  UserProfileDTO,
  UploadTokenResponse,
  ChangeDisplayNameRequest,
  UpdateProfilePictureRequest,
} from '../dto/profile.dto';

@Injectable({
  providedIn: 'root',
})
export class ProfileService {
  private readonly _http = inject(HttpClient);
  private readonly _apiUrl = environment.apiUrl;


  public getUserProfile(userId: string): Observable<UserProfileDTO> {
    return this._http.get<UserProfileDTO>(
      `${this._apiUrl}/Profile/${userId}`,
      { withCredentials: true }
    );
  }


  public changeDisplayName(displayName: string): Observable<void> {
    const payload: ChangeDisplayNameRequest = { displayName };
    return this._http.put<void>(
      `${this._apiUrl}/Profile/display-name`,
      payload,
      { withCredentials: true }
    );
  }


  public getUploadToken(fileName: string, contentType: string): Observable<UploadTokenResponse> {
    return this._http.get<UploadTokenResponse>(
      `${this._apiUrl}/Profile/picture-upload-token`,
      {
        params: { fileName, contentType },
        withCredentials: true,
      }
    );
  }


  public uploadToBlob(sasUrl: string, file: File): Observable<void> {
    const headers = new HttpHeaders({
      'x-ms-blob-type': 'BlockBlob',
      'Content-Type': file.type,
    });
    return this._http.put<void>(sasUrl, file, { headers });
  }


  public updateProfilePictureUrl(blobName: string): Observable<void> {
    const payload: UpdateProfilePictureRequest = { blobName };
    return this._http.put<void>(
      `${this._apiUrl}/Profile/picture-url`,
      payload,
      { withCredentials: true }
    );
  }

  /**
   * Orchestrates the complete avatar upload flow:
   * 1. Request SAS upload token from backend
   * 2. PUT file directly to Azure Blob Storage
   * 3. Update profile picture URL in the database
   */
  public async uploadAvatar(file: File): Promise<void> {
    const tokenResponse = await firstValueFrom(this.getUploadToken(file.name, file.type));
    await firstValueFrom(this.uploadToBlob(tokenResponse.uploadUrl, file));
    await firstValueFrom(this.updateProfilePictureUrl(tokenResponse.blobName));
  }
}
